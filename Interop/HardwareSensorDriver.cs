namespace EndfieldChargePlus.Interop;

internal static class HardwareSensorDriver
{
    internal sealed record NativeSensorStatus(string Platform,string Description);
    internal static NativeSensorStatus Inspect()=>new(OperatingSystem.IsMacOS() ? "macOS" : "Linux",
        LocalizationManager.Text("使用系统原生硬件接口。数据取决于硬件、驱动和当前权限；不需要 PawnIO。","Using native system hardware interfaces. Available data depends on hardware, drivers and permissions; PawnIO is not required."));
    internal static Task<int> InstallAsync(CancellationToken token)=>throw new PlatformNotSupportedException("PawnIO is only available on Windows.");
}
