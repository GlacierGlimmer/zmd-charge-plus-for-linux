using System;
using Microsoft.Win32;
using EndfieldChargePlus.Diagnostics;

namespace EndfieldChargePlus.Settings;

public static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Endfield Charge Plus";

    public static void Apply(bool enabled)
    {
        if (OperatingSystem.IsLinux())
        {
            try { ApplyLinux(enabled); }
            catch (Exception ex) { AppLog.Error("Failed to update Linux autostart entry.", ex); }
            return;
        }
        if (!OperatingSystem.IsWindows()) return;

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            if (key is null) return;

            if (!enabled)
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
                return;
            }

            string? path = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(path)) return;
            key.SetValue(ValueName, $"\"{path}\" --autostart", RegistryValueKind.String);
        }
        catch
        {
            // Startup registration failure should never stop the HUD from running.
        }
    }

    internal static string AutostartPath => Path.Combine(
        Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") is { } config && Path.IsPathRooted(config)
            ? config : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config"),
        "autostart", "endfield-charge-plus-for-linux.desktop");

    private static void ApplyLinux(bool enabled)
    {
        var legacy = Path.Combine(Path.GetDirectoryName(AutostartPath)!, "endfield-charge-plus.desktop");
        if (File.Exists(legacy) && File.ReadAllText(legacy).Contains("Name=Endfield Charge Plus\n", StringComparison.Ordinal))
            File.Delete(legacy);
        if (!enabled) { File.Delete(AutostartPath); return; }
        // AppImage mounts are temporary. Persist the original AppImage path instead.
        string? executable = Environment.GetEnvironmentVariable("APPIMAGE");
        if (string.IsNullOrEmpty(executable)) executable = Environment.ProcessPath;
        if (string.IsNullOrEmpty(executable)) return;
        string command = QuoteDesktopArgument(executable);
        if (Path.GetFileNameWithoutExtension(executable) == "dotnet")
            command += " " + QuoteDesktopArgument(typeof(StartupManager).Assembly.Location);
        Directory.CreateDirectory(Path.GetDirectoryName(AutostartPath)!);
        string content = "[Desktop Entry]\nType=Application\nName=" + ProductInfo.Name + "\nExec=" + command
            + " --autostart\nIcon=endfield-charge-plus-for-linux\nTerminal=false\nX-GNOME-Autostart-enabled=true\n";
        File.WriteAllText(AutostartPath + ".tmp", content);
        File.Move(AutostartPath + ".tmp", AutostartPath, overwrite: true);
    }

    internal static string QuoteDesktopArgument(string value)
    {
        if (value.Any(c => c is '\n' or '\r' or '\0')) throw new ArgumentException("Invalid desktop entry path.");
        // Desktop entry string escaping is applied after Exec's quoted-argument escaping.
        var escaped = value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("`", "\\`").Replace("$", "\\$")
            .Replace("%", "%%");
        return "\"" + escaped.Replace("\\", "\\\\") + "\"";
    }
}
