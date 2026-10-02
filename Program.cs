using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using Avalonia;
using EndfieldChargePlus.Diagnostics;

namespace EndfieldChargePlus;

internal static class Program
{
    private const string SingleInstanceMutexName = @"Local\EndfieldChargePlus.SingleInstance";
    internal const string ActivationEventName = @"Local\EndfieldChargePlus.Activate";

    private static Mutex? _singleInstanceMutex;
    private static EventWaitHandle? _activationEvent;

    internal static EventWaitHandle? ActivationEvent => _activationEvent;
    internal static bool IsAutoStart { get; private set; }
    internal static string LinuxInstanceName { get; } = "EndfieldChargePlus-" + Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(Interop.AppPaths.DataDirectory)))[..24];

    [STAThread]
    public static void Main(string[] args)
    {
        IsAutoStart = args.Any(a => string.Equals(a, "--autostart", StringComparison.OrdinalIgnoreCase));

        bool isPrimaryInstance;
        try
        {
            _singleInstanceMutex = new Mutex(initiallyOwned: true,
                OperatingSystem.IsWindows() ? SingleInstanceMutexName : LinuxInstanceName, out isPrimaryInstance);
        }
        catch
        {
            // If the OS refuses named-kernel-object creation for an unexpected reason,
            // keep the application usable rather than failing before Avalonia starts.
            isPrimaryInstance = true;
        }

        if (!isPrimaryInstance)
        {
            // Never start a second background/HUD process. A normal launch asks the
            // existing instance to surface Settings; an autostart launch exits silently.
            if (!IsAutoStart)
            {
                try
                {
                    if (OperatingSystem.IsWindows())
                    {
                        using var activation = EventWaitHandle.OpenExisting(ActivationEventName);
                        activation.Set();
                    }
                    else
                    {
                        using var pipe = new NamedPipeClientStream(".", LinuxInstanceName, PipeDirection.Out, PipeOptions.CurrentUserOnly);
                        pipe.Connect(5000);
                        pipe.WriteByte(1);
                    }
                }
                catch
                {
                    // The first instance may still be inside very early startup. The key
                    // requirement is still satisfied: this second process exits.
                }
            }
            return;
        }

        try
        {
            if (OperatingSystem.IsWindows())
                _activationEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivationEventName);
        }
        catch
        {
            _activationEvent = null;
        }

        AppLog.Initialize();
        AppLog.Info($"{ProductInfo.Name} process started. Version={typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "unknown"}");

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            AppLog.Fatal("Unhandled AppDomain exception.", e.ExceptionObject as Exception);
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            AppLog.Error("Unobserved task exception.", e.Exception);
            e.SetObserved();
        };

        try
        {
            if (OperatingSystem.IsLinux()) Customization.LinuxVariableCatalog.Refresh();
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            AppLog.Fatal("Application terminated by an unhandled startup/runtime exception.", ex);
            throw;
        }
        finally
        {
            AppLog.Info($"{ProductInfo.Name} process ended.");

            try { _activationEvent?.Dispose(); } catch { }
            _activationEvent = null;

            try { _singleInstanceMutex?.ReleaseMutex(); } catch { }
            try { _singleInstanceMutex?.Dispose(); } catch { }
            _singleInstanceMutex = null;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        Interop.LinuxDesktop.PrepareClipboard();
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
