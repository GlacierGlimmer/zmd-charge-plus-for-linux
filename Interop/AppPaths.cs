namespace EndfieldChargePlus.Interop;

internal static class AppPaths
{
    internal static string DataDirectory
    {
        get
        {
            if (!OperatingSystem.IsLinux())
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EndfieldChargePlus");
            // GetFolderPath can return an empty string when the XDG directory doesn't exist yet.
            // Compute an absolute path explicitly so a first launch never writes beside the app.
            string? configured = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            string data = !string.IsNullOrEmpty(configured) && Path.IsPathRooted(configured)
                ? configured : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
            return Path.Combine(data, "EndfieldChargePlus");
        }
    }
}
