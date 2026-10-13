// Cache only the exact published package referenced by the signed APT index.
// Older packages continue to use the existing R2 bucket.
const releasePackages = {
  "/pool/main/e/endfield-charge-plus/EndfieldChargePlusForLinux-v0.1.1-linux-x64.deb": {
    url: "https://github.com/GlacierGlimmer/zmd-charge-plus-for-linux/releases/download/v0.1.1/EndfieldChargePlusForLinux-v0.1.1-linux-x64.deb",
    size: 34366734,
    sha256: "cac2236d1a1ddc527267c4535062055cbdd90f607e1a266cb28ff69f50428865",
  },
};

async function cacheReleasePackage(bucket, key, release) {
  const existing = await bucket.head(key);
  if (existing?.size === release.size && existing.customMetadata?.sha256 === release.sha256) return;
  const response = await fetch(release.url);
  if (!response.ok || !response.body || Number(response.headers.get("content-length")) !== release.size)
    throw new Error("Unexpected release package response");
  const temporary = "staging/" + crypto.randomUUID();
  const [upload, checksum] = response.body.tee();
  const digest = new crypto.DigestStream("SHA-256");
  try {
    // Stream both copies; never retain the 34 MB package in Worker memory.
    const [object] = await Promise.all([
      bucket.put(temporary, upload),
      checksum.pipeTo(digest),
    ]);
    const sha = Array.from(new Uint8Array(await digest.digest), x => x.toString(16).padStart(2, "0")).join("");
    if (object.size !== release.size || sha !== release.sha256) throw new Error("Release checksum mismatch");
    const verified = await bucket.get(temporary);
    if (!verified) throw new Error("Verified staging package disappeared");
    await bucket.put(key, verified.body, {
      httpMetadata: { contentType: "application/vnd.debian.binary-package" },
      customMetadata: { sha256: release.sha256 },
    });
  } finally {
    await bucket.delete(temporary);
  }
}

export default {
  async fetch(request, env) {
    const url = new URL(request.url);

    if (!url.pathname.startsWith("/pool/")) {
      return env.ASSETS.fetch(request);
    }

    if (request.method !== "GET" && request.method !== "HEAD") {
      return new Response("Method Not Allowed", {
        status: 405,
        headers: { Allow: "GET, HEAD" },
      });
    }

    const key = decodeURIComponent(url.pathname.slice(1));
    if (releasePackages[url.pathname]) {
      try {
        await cacheReleasePackage(env.APT_PACKAGES, key, releasePackages[url.pathname]);
      } catch (error) {
        console.error("APT package cache failed", error instanceof Error ? error.message : "Unknown failure");
        return new Response("Package temporarily unavailable", { status: 503, headers: { "Retry-After": "30" } });
      }
    }

    if (request.method === "HEAD") {
      const object = await env.APT_PACKAGES.head(key);
      if (object === null) {
        return new Response("Not Found", { status: 404 });
      }

      const headers = new Headers();
      object.writeHttpMetadata(headers);
      headers.set("etag", object.httpEtag);
      headers.set("accept-ranges", "bytes");
      headers.set("content-length", String(object.size));
      if (!headers.has("content-type")) {
        headers.set("content-type", "application/vnd.debian.binary-package");
      }

      return new Response(null, { status: 200, headers });
    }

    const object = await env.APT_PACKAGES.get(key, {
      onlyIf: request.headers,
      range: request.headers,
    });

    if (object === null) {
      return new Response("Not Found", { status: 404 });
    }

    const headers = new Headers();
    object.writeHttpMetadata(headers);
    headers.set("etag", object.httpEtag);
    headers.set("accept-ranges", "bytes");

    if (!("body" in object)) {
      return new Response(null, { status: 412, headers });
    }

    let status = 200;

    if (object.range) {
      status = 206;
      headers.set(
        "content-range",
        `bytes ${object.range.offset}-${object.range.offset + object.range.length - 1}/${object.size}`
      );
      headers.set("content-length", String(object.range.length));
    } else {
      headers.set("content-length", String(object.size));
    }

    if (!headers.has("content-type")) {
      headers.set("content-type", "application/vnd.debian.binary-package");
    }

    return new Response(object.body, { status, headers });
  },
};
