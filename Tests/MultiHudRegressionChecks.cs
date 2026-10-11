using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using EndfieldChargePlus.Customization;
using EndfieldChargePlus.Settings;
using EndfieldChargePlus.Views;

internal static class MultiHudRegressionChecks
{
    internal static async Task Run(Action<bool,string> check)
    {
        var hud=new HudWindow();
        string family=OperatingSystem.IsMacOS() ? "Menlo" : "DejaVu Sans Mono";
        hud.ApplySettings(new() { HudFontFamily=family });
        foreach(var name in new[] { "TagLineText","TitleText","PrimaryText","SecondaryText","RightText","RightSuffixText" })
            check(hud.FindControl<TextBlock>(name)!.FontFamily.Name==family,"Installed HUD font is applied before first Show: "+name);
        var profile=new HudProfile { Id="multi-regression",Name="Multi regression",PrimaryTemplate="LIVE",RightTemplate="50",ProgressVariable="=50",AnimationMode="Simple" };
        var settings=new AppSettings { AlwaysVisible=true,PersistentLayer=PersistentHudLayer.Topmost,SamplingIntervalSeconds=.1,
            CustomHud=new() { ActiveProfileId=profile.Id,Profiles=new(){profile},CycleProfileIds=new(){profile.Id} } };
        using var runtime=new CustomHudRuntime(hud);
        hud.ApplySettings(settings);runtime.ApplySettings(settings);runtime.Start();
        await Task.Delay(900);
        check(hud.IsVisible,"Primary native HUD starts with real contents");
        for(int pass=0;pass<15;pass++)
        {
            var children=Enumerable.Range(0,15).Select(i=>new HudInstanceSettings { Id=$"child-{i}",Settings=settings with { MonitorIndex=i%2,HudOffsetY=i*3,HudFontFamily=family } }).ToList();
            runtime.ApplySettings(settings with { MultiHudEnabled=true,HudInstances=children });
            await Task.Delay(20);
            var locate=runtime.HighlightInstanceAsync("child-12","Native stress");
            runtime.ApplySettings(settings with { MultiHudEnabled=false });
            await locate;
        }
        await runtime.ApplySettingsWithTransitionAsync(settings);
        check(hud.IsVisible && hud.FindControl<TextBlock>("PrimaryText")!.Text=="LIVE","Native primary HUD survives 225 secondary HUD creation/highlight/removal cycles");
        runtime.Dispose();hud.Close();await Task.Delay(200);
        check(hud.IsClosed,"Closed native HUD remains disposed after asynchronous operations drain");
    }
}
