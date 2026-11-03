namespace EndfieldChargePlus.Interop;

internal static class AppPaths
{
    internal static string DataDirectory
    {
        get
        {
            if (!OperatingSystem.IsLinux())
            {
                string local=Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if(!Path.IsPathFullyQualified(local))
                    throw new InvalidOperationException("The current user's data directory is unavailable. Configuration cannot use a relative application path.");
                return Path.Combine(local,"EndfieldChargePlus");
            }
            // GetFolderPath can return an empty string when the XDG directory doesn't exist yet.
            // Compute an absolute path explicitly so a first launch never writes beside the app.
            string? configured = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            string data = !string.IsNullOrEmpty(configured) && Path.IsPathRooted(configured)
                ? configured : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
            if(!Path.IsPathFullyQualified(data))
                throw new InvalidOperationException("The current user's data directory is unavailable. Configuration cannot use a relative application path.");
            return Path.Combine(data, "EndfieldChargePlus");
        }
    }
}
