using Avalonia.Controls;
namespace EndfieldChargePlus.Customization;
public partial class HudCustomizerView
{
    private string _frameDisplaySelected="", _frameWindowSelected="";
    private bool _frameNeedsDisplay, _frameNeedsWindow;
    private void LoadFrameOptions(HudProfile profile)
    {
        var keys=HudProfileRenderer.GetRequiredVariables(profile);
        _frameNeedsDisplay=keys.Any(k=>k.StartsWith("frame.display."));
        _frameNeedsWindow=keys.Any(k=>k.StartsWith("frame.window."));
        FrameOptionsPanel.IsVisible=_frameNeedsDisplay || _frameNeedsWindow;
        if(!FrameOptionsPanel.IsVisible) return;
        FrameDisplayOptions.IsVisible=_frameNeedsDisplay; FrameWindowOptions.IsVisible=_frameNeedsWindow;
        _frameDisplaySelected=profile.FrameDisplayId; _frameWindowSelected=profile.FrameWindowId;
        FrameFullScaleBox.Value=(decimal)profile.FrameFullScaleFps;
        FrameDisplayTargetCombo.SelectedItem=null; FrameWindowTargetCombo.SelectedItem=null;
        RefreshFrameTargets();
        FrameCaptureHint.Text=LocalizationManager.Text("读取 X11／XWayland Present 完成事件。原生 Wayland、未使用 Present 的应用或缺少 libxpresent1 时无法采集。不是绘制次数或固定刷新率。","Reads X11/XWayland Present completion events. Native Wayland, applications without Present, or missing libxpresent1 cannot be measured. Paint-operation counts and fixed refresh rates are excluded.");
    }
    private void RefreshFrameTargets()
    {
        if(_frameNeedsDisplay) _frameDisplaySelected=FillFrameTargets(FrameDisplayTargetCombo,"display",_frameDisplaySelected);
        if(_frameNeedsWindow) _frameWindowSelected=FillFrameTargets(FrameWindowTargetCombo,"window",_frameWindowSelected);
    }
    private static string FillFrameTargets(ComboBox combo,string kind,string previous)
    {
        string selected=(combo.SelectedItem as FrameTarget)?.Id ?? previous;
        var targets=kind=="window" ? FrameRateBackend.Windows() : FrameRateBackend.Displays();
        var items=new[] { new FrameTarget("",kind=="display" ? LocalizationManager.Text("主显示器（自动）","Primary display (auto)") : LocalizationManager.Text("请选择窗口","Select a window")) }
            .Concat(targets.GroupBy(t=>t.Id).Select(g=>g.First())).ToArray();
        if (!items.Any(t=>t.Id==selected)) selected="";
        combo.ItemsSource=items;
        combo.SelectedItem=items.First(t=>t.Id==selected);
        return selected;
    }
}
