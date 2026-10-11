using System.Text.Json;
using EndfieldChargePlus.Customization;
using EndfieldChargePlus.Settings;

internal static class FeatureRegressionChecks
{
    internal static void Run(Action<bool,string> check)
    {
        var frames=new FrameEventWindow();
        check(frames.Read(100)==null,"FPS cannot fabricate a zero before its first event.");
        frames.Add(1,100); frames.Add(1,101);
        check(frames.Read(102)==null,"A duplicate frame timestamp is not a second frame.");
        frames.Add(1.02,120); frames.Add(1.04,140);
        check(Math.Abs(frames.Read(140)!.Value-50)<0.001,"Actual presentation timestamps determine FPS.");
        check(frames.Read(2200)==0,"An observed but subsequently idle stream has real zero FPS.");
        frames.Add(double.NaN,2201); frames.Add(2,2202);
        check(frames.Read(2203)==null,"A restarting stream requires two real samples.");
        var metrics=new Dictionary<string,object?>();
        FrameRateMetrics.Add(metrics,"window",new(60,"Test","OK"),120);
        check(Convert.ToDouble(metrics["frame.window.percent"])==50,"The configured full-scale FPS defines 100%.");
        FrameRateMetrics.Add(metrics,"window",new(null,"Test","Closed"),120);
        check(metrics["frame.window.fps"]==null && metrics["frame.window.percent"]==null,"An unavailable source cannot become zero or an invented percentage.");
        var csv=FrameRateMetrics.ParseCsv("\"name, \"\"quoted\"\"\",123,0x01");
        check(csv.Count==3 && csv[0]=="name, \"quoted\"","CSV parsing preserves quoted process names.");
        var settings=CustomHudSettings.CreateDefault();
        var target=settings.Profiles.Single(p=>p.BuiltInKey=="system.window-fps");
        var configured=target with { FrameWindowId="abc:12",FrameDisplayId="display-2",FrameFullScaleFps=165 };
        var normalized=HudSettingsNormalizer.Normalize(settings with { Profiles=settings.Profiles.Select(p=>p.Id==target.Id ? configured : p).ToList() });
        var kept=normalized.Profiles.Single(p=>p.Id==target.Id);
        check(kept.FrameWindowId=="abc:12" && kept.FrameDisplayId=="display-2" && kept.FrameFullScaleFps==165,"Built-in FPS target settings survive configuration normalization.");
        check(settings.Profiles.Any(p=>p.BuiltInKey=="system.display-fps") && settings.CycleProfileIds!.All(id=>!settings.Profiles.Single(p=>p.Id==id).BuiltInKey.EndsWith("-fps")),"New capture profiles are available without silently joining the existing carousel.");
        var scheme=new HudProfile { PrimaryTemplate="{frame.window.fps}",SecondaryTemplate="",RightTemplate="",ProgressVariable="frame.window.percent",TitleTemplate="",TaglineTemplate="" };
        check(HudProfileRenderer.GetUnavailableVariables(scheme,metrics).Count>0,"Missing profile data is detected.");
        FrameRateMetrics.Add(metrics,"window",new(0,"Test","OK"),120);
        check(HudProfileRenderer.GetUnavailableVariables(scheme,metrics).Count==0,"Valid zero measurements do not produce a data error.");
        var invalid=scheme with { PrimaryTemplate="{= 1 / 0}",ProgressVariable="= 0 / 0" };
        check(HudProfileRenderer.GetUnavailableVariables(invalid,metrics).Count>0,"Invalid numeric expressions are reported.");
        var multi=new AppSettings { MultiHudEnabled=true,HudFontFamily="Inter",SamplingIntervalSeconds=.25,
            HudInstances=new() { new() { Name="Second display",Settings=new() { MonitorIndex=1,HudOffsetX=-100,AlwaysVisible=true,ClickToCycle=true,HudFontFamily="Arial" } } } };
        var restored=JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(multi))!;
        check(restored.MultiHudEnabled && restored.HudInstances.Count==1 && restored.HudInstances[0].Settings.MonitorIndex==1 && restored.HudInstances[0].Settings.HudFontFamily=="Arial" && restored.HudFontFamily=="Inter","Multi-HUD configuration round trips independently, including monitor, font and click behavior.");
    }
}
