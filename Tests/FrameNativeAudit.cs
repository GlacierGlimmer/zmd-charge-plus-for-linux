using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using EndfieldChargePlus.Customization;

internal static class FrameNativeAudit
{
    private sealed class AuditApp : Application { }
    internal static int Run()
    {
        bool passed=false;
        AppBuilder.Configure<AuditApp>().UsePlatformDetect().AfterSetup(_ => DispatcherTimer.RunOnce(async () =>
        {
            var life=(IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!;
            var anchor=new Window { Title="ECP capture audit",Width=20,Height=20,ShowInTaskbar=false };
            life.MainWindow=anchor; anchor.Show();
            try
            {
                FrameTarget? target=null;
                for(int i=0;i<100 && target is null;i++) { target=FrameRateBackend.Windows().FirstOrDefault(w=>w.Name.Contains("real Present fixture")); await Task.Delay(40); }
                if(target is null) throw new Exception("Native test window was not enumerated.");
                double? windowFps=null,displayFps=null;
                for(int i=0;i<120;i++) {
                    var window=FrameRateBackend.Read("window",target.Id);
                    var display=FrameRateBackend.Read("display","");
                    if(window.Fps is >20 and <40) windowFps=window.Fps;
                    if(display.Fps is >20 and <40) displayFps=display.Fps;
                    await Task.Delay(25);
                }
                if(windowFps is null || displayFps is null) throw new Exception($"Expected actual 30 FPS; window={windowFps}, display={displayFps}, status={FrameRateBackend.Read("window",target.Id).Status}");
                Console.WriteLine($"PASS: live XPresent frames: window={windowFps:F2}, display={displayFps:F2}; source submits 30 FPS independently of the display refresh setting.");
                if(FrameRateBackend.Read("window","invalid-id").Fps is not null) throw new Exception("Invalid target became a fake reading.");
                passed=true;
            }
            catch(Exception ex) { Console.Error.WriteLine(ex); }
            finally { FrameRateBackend.Stop(); life.Shutdown(passed ? 0 : 1); }
        },TimeSpan.FromMilliseconds(100))).StartWithClassicDesktopLifetime(Array.Empty<string>());
        return passed ? 0 : 1;
    }
}
