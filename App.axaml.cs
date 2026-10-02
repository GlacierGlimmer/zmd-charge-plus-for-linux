using System.Threading;
using System.IO.Pipes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.Platform;
using EndfieldChargePlus.Customization;
using EndfieldChargePlus.Diagnostics;
using EndfieldChargePlus.Interop;
using EndfieldChargePlus.Settings;
using EndfieldChargePlus.Views;

namespace EndfieldChargePlus;

public partial class App : Application
{
    private HudWindow? _hud;
    private CustomHudRuntime? _runtime;
    private WindowsTrayIcon? _tray;
    private TrayIcon? _linuxTray;
    private TrayMenuWindow? _trayMenu;
    private SettingsWindow? _settingsWindow;
    private IClassicDesktopStyleApplicationLifetime? _desktop;
    private CancellationTokenSource? _activationListenerCts;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _desktop = desktop;
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;

            AppLog.Info("Avalonia framework initialization completed; loading settings.");
            var settings = SettingsManager.Load();
            LocalizationManager.Initialize(settings.UiLanguage);
            StartupManager.Apply(settings.StartWithWindows);

            _hud = new HudWindow();
            _hud.ApplySettings(settings);

            _runtime = new CustomHudRuntime(_hud);
            _runtime.ApplySettings(settings);
            _runtime.Start();

            // Normal interactive launch opens Settings. Windows autostart stays silent:
            // only the tray icon and startup HUD presentation are shown.
            if (!Program.IsAutoStart)
            {
                _settingsWindow = CreateSettingsWindow(settings);
                desktop.MainWindow = _settingsWindow;
            }
            else
            {
                _settingsWindow = null;
                AppLog.Info("Autostart launch detected; Settings window will remain hidden.");
            }

            SetupTrayIcon();
            LocalizationManager.LanguageChanged += RefreshLinuxTray;
            StartSecondInstanceActivationListener();
            _ = RunStartupUpdateCheckAsync();
            desktop.Exit += OnDesktopExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private SettingsWindow CreateSettingsWindow(AppSettings settings)
    {
        var window = new SettingsWindow(settings, _hud!, _runtime!);
        window.Closed += (_, _) =>
        {
            if (ReferenceEquals(_settingsWindow, window))
                _settingsWindow = null;
        };
        return window;
    }

    private void OpenSettingsWindow()
    {
        if (_desktop is null || _hud is null || _runtime is null)
            return;

        if (_settingsWindow is { IsVisible: true })
        {
            if (_settingsWindow.WindowState == Avalonia.Controls.WindowState.Minimized)
                _settingsWindow.WindowState = Avalonia.Controls.WindowState.Normal;
            _settingsWindow.Activate();
            return;
        }

        var settings = SettingsManager.Load();
        _settingsWindow = CreateSettingsWindow(settings);
        _desktop.MainWindow = _settingsWindow;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private void StartSecondInstanceActivationListener()
    {
        var activationEvent = Program.ActivationEvent;
        if (activationEvent is null && OperatingSystem.IsWindows()) return;

        _activationListenerCts?.Cancel();
        _activationListenerCts?.Dispose();
        _activationListenerCts = new CancellationTokenSource();
        var token = _activationListenerCts.Token;

        _ = Task.Run(async () =>
        {
            if (!OperatingSystem.IsWindows())
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        using var pipe = new NamedPipeServerStream(Program.LinuxInstanceName, PipeDirection.In, 1,
                            PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                        await pipe.WaitForConnectionAsync(token);
                        var data = new byte[1];
                        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                        timeout.CancelAfter(TimeSpan.FromSeconds(2));
                        if (await pipe.ReadAsync(data, timeout.Token) == 1 && data[0] == 1)
                            await Dispatcher.UIThread.InvokeAsync(OpenSettingsWindow);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
                    catch (Exception ex)
                    {
                        AppLog.Warn($"Activation pipe: {ex.Message}");
                        try { await Task.Delay(250, token); } catch (OperationCanceledException) { break; }
                    }
                }
                return;
            }
            while (!token.IsCancellationRequested)
            {
                bool signaled;
                try
                {
                    signaled = activationEvent!.WaitOne(500);
                }
                catch
                {
                    break;
                }

                if (!signaled) continue;

                try
                {
                    await Dispatcher.UIThread.InvokeAsync(OpenSettingsWindow);
                }
                catch
                {
                    // Application may already be shutting down.
                }
            }
        }, token);
    }

    private async Task RunStartupUpdateCheckAsync()
    {
        AppLog.Info("Startup update check started.");
        SettingsWindow.UpdateCheckResult result = await SettingsWindow.CheckForUpdatesAsync();

        AppLog.Info($"Startup update check finished: {result.StatusText}");

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_settingsWindow is not null)
                _settingsWindow.ApplyUpdateCheckResult(result);

            if (result.HasUpdate)
            {
                string latest = result.LatestVersionText
                    .Replace("最新版本：", "", StringComparison.Ordinal)
                    .Replace("Latest: ", "", StringComparison.Ordinal);
                _tray?.ShowNotification(
                    LocalizationManager.Text("Endfield Charge Plus 有新版本", "Endfield Charge Plus update available"),
                    LocalizationManager.Text($"发现 {latest}。可在“关于”页面查看更新状态。", $"Found {latest}. See About for update details."));
            }
        });
    }

    private void SetupTrayIcon()
    {
        if (!OperatingSystem.IsWindows())
        {
            SetupLinuxTrayIcon();
            return;
        }

        try
        {
            _tray = new WindowsTrayIcon(
                ToggleTrayMenu,
                () =>
                {
                    CloseTrayMenu();
                    OpenSettingsWindow();
                },
                "Endfield Charge Plus");
        }
        catch (Exception ex)
        {
            // The main application remains usable even if Explorer/tray creation fails.
            AppLog.Error("Failed to create the Windows tray icon.", ex);
            _tray = null;
        }
    }

    private void SetupLinuxTrayIcon()
    {
        try
        {
            _linuxTray?.Dispose();
            var menu = new NativeMenu();
            var preview = new NativeMenuItem(LocalizationManager.Text("预览", "Preview"));
            preview.Click += (_, _) => { if (_runtime is not null) _ = _runtime.PreviewActiveAsync(); };
            var settings = new NativeMenuItem(LocalizationManager.Text("设置", "Settings"));
            settings.Click += (_, _) => OpenSettingsWindow();
            var exit = new NativeMenuItem(LocalizationManager.Text("退出", "Exit"));
            exit.Click += (_, _) => _desktop?.Shutdown();
            menu.Items.Add(preview);
            menu.Items.Add(settings);
            menu.Items.Add(new NativeMenuItemSeparator());
            menu.Items.Add(exit);
            using var icon = AssetLoader.Open(new Uri("avares://EndfieldChargePlus/Assets/tray_bolt.png"));
            _linuxTray = new TrayIcon { Icon = new WindowIcon(icon), ToolTipText = ProductInfo.Name, Menu = menu, IsVisible = true };
            _linuxTray.Clicked += (_, _) => OpenSettingsWindow();
            TrayIcon.SetIcons(this, new TrayIcons { _linuxTray });
        }
        catch (Exception ex) { AppLog.Error("Failed to create the Linux tray icon.", ex); }
    }

    private void RefreshLinuxTray()
    {
        if (OperatingSystem.IsLinux()) SetupLinuxTrayIcon();
    }

    private void ToggleTrayMenu()
    {
        if (_trayMenu is { IsVisible: true })
        {
            CloseTrayMenu();
            return;
        }

        var menu = new TrayMenuWindow();
        _trayMenu = menu;
        menu.Closed += (_, _) =>
        {
            if (ReferenceEquals(_trayMenu, menu))
                _trayMenu = null;
        };

        menu.PreviewClicked += () =>
        {
            CloseTrayMenu();
            if (_runtime is not null)
                _ = _runtime.PreviewActiveAsync();
        };

        menu.SettingsClicked += () =>
        {
            CloseTrayMenu();
            OpenSettingsWindow();
        };

        menu.ExitClicked += () =>
        {
            CloseTrayMenu();
            _desktop?.Shutdown();
        };

        menu.ShowAtTray();
    }

    private void CloseTrayMenu()
    {
        var menu = _trayMenu;
        _trayMenu = null;
        if (menu is not null)
        {
            try { menu.Close(); }
            catch { }
        }
    }

    private void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        LocalizationManager.LanguageChanged -= RefreshLinuxTray;
        AppLog.Info("Desktop lifetime is exiting.");
        _activationListenerCts?.Cancel();
        _activationListenerCts?.Dispose();
        _activationListenerCts = null;
        CloseTrayMenu();
        _tray?.Dispose();
        _tray = null;
        _linuxTray?.Dispose();
        _linuxTray = null;
        _runtime?.Dispose();
        _runtime = null;
        _hud?.Close();
        _hud = null;
        LinuxDesktop.Close();
    }
}
