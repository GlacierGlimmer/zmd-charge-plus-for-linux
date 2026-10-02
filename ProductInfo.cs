namespace EndfieldChargePlus;

internal static class ProductInfo
{
    internal const string LinuxName = "Endfield Charge Plus For Linux";
    internal static string Name => OperatingSystem.IsLinux() ? LinuxName : "Endfield Charge Plus";
    internal static string Brand(string value)
    {
        if (!OperatingSystem.IsLinux()) return value;
        return value.Replace(LinuxName, "Endfield Charge Plus", StringComparison.Ordinal)
            .Replace("Endfield Charge Plus", LinuxName, StringComparison.Ordinal)
            .Replace("ENDFIELD CHARGE PLUS FOR LINUX", "ENDFIELD CHARGE PLUS", StringComparison.Ordinal)
            .Replace("ENDFIELD CHARGE PLUS", "ENDFIELD CHARGE PLUS FOR LINUX", StringComparison.Ordinal);
    }
}
