using System;
using System.Security.Cryptography;
using System.Text;
using EndfieldChargePlus.Settings;

namespace EndfieldChargePlus.Customization;

public static class SecretStore
{
    public static string Protect(string? plain)
    {
        if (string.IsNullOrWhiteSpace(plain)) return "";
        try
        {
            var bytes = Encoding.UTF8.GetBytes(plain);
            if (OperatingSystem.IsLinux())
            {
                using var aes = new AesGcm(GetLinuxKey(create: true), 16);
                var nonce = RandomNumberGenerator.GetBytes(12);
                var encrypted = new byte[bytes.Length];
                var tag = new byte[16];
                aes.Encrypt(nonce, bytes, encrypted, tag);
                return "linux-aesgcm-v1:" + Convert.ToBase64String(nonce.Concat(tag).Concat(encrypted).ToArray());
            }
            if (!OperatingSystem.IsWindows()) return "";
            var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(protectedBytes);
        }
        catch (Exception ex)
        {
            Diagnostics.AppLog.Error("Failed to protect the API key.", ex);
            return "";
        }
    }

    public static string Unprotect(string? encoded)
    {
        if (string.IsNullOrWhiteSpace(encoded)) return "";
        try
        {
            if (encoded.StartsWith("linux-aesgcm-v1:", StringComparison.Ordinal))
            {
                if (!OperatingSystem.IsLinux()) return "";
                var data = Convert.FromBase64String(encoded[16..]);
                if (data.Length < 28) return "";
                using var aes = new AesGcm(GetLinuxKey(create: false), 16);
                var decrypted = new byte[data.Length - 28];
                aes.Decrypt(data.AsSpan(0, 12), data.AsSpan(28), data.AsSpan(12, 16), decrypted);
                return Encoding.UTF8.GetString(decrypted);
            }
            if (!OperatingSystem.IsWindows()) return "";
            var bytes = Convert.FromBase64String(encoded);
            var plain = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plain);
        }
        catch (Exception ex)
        {
            Diagnostics.AppLog.Error("Failed to decrypt the API key.", ex);
            return "";
        }
    }

    private static byte[] GetLinuxKey(bool create)
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException();
        string directory = Path.Combine(SettingsManager.SettingsDirectory, "secrets");
        string path = Path.Combine(directory, "master.key");
        if (create)
        {
            Directory.CreateDirectory(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            if (!File.Exists(path))
            {
                try
                {
                    using var file = new FileStream(path, new FileStreamOptions
                    {
                        Mode = FileMode.CreateNew, Access = FileAccess.Write,
                        UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite,
                    });
                    file.Write(RandomNumberGenerator.GetBytes(32));
                }
                catch (IOException) when (File.Exists(path)) { }
            }
        }
        var key = File.ReadAllBytes(path);
        if (key.Length != 32) throw new CryptographicException("Invalid local encryption key.");
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        if (File.GetUnixFileMode(path) != (UnixFileMode.UserRead | UnixFileMode.UserWrite))
            throw new CryptographicException("The key filesystem must support private Unix file permissions.");
        return key;
    }
}
