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
