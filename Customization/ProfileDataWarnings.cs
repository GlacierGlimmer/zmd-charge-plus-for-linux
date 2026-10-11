using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
namespace EndfieldChargePlus.Customization;

/// <summary>One live warning window for the entire active failure episode, shared by all HUDs.</summary>
public static class ProfileDataWarnings
{
    private sealed record Incident(string Scope,string Name,string[] Keys,long Since);
    private static readonly object Gate=new();
    private static readonly Dictionary<string,Incident> Incidents=new();
    private static Window? _dialog;
    private static TextBlock? _details;
    private static DispatcherTimer? _mergeTimer;
    private static bool _acknowledged;
    public static void Check(HudProfile profile,IReadOnlyDictionary<string,object?> values,string scope="")
    {
        var missing=HudProfileRenderer.GetUnavailableVariables(profile,values).OrderBy(k=>k).ToArray();
        string key=scope+":"+profile.Id;
        lock(Gate) {
            // A HUD has one current profile. Retired profiles must not stay in the warning.
            foreach(var old in Incidents.Where(p=>p.Value.Scope==scope && p.Key!=key).Select(p=>p.Key).ToArray()) Incidents.Remove(old);
            if(missing.Length==0) Incidents.Remove(key);
            else {
                long since=Incidents.TryGetValue(key,out var old) ? old.Since : Environment.TickCount64;
                Incidents[key]=new(scope,BuiltInProfileLocalization.DisplayName(profile),missing,since);
            }
            if(Incidents.Count==0) _acknowledged=false;
        }
        Dispatcher.UIThread.Post(ScheduleRefresh);
    }
    public static void Forget(string scope)
    {
        lock(Gate) {
            foreach(var key in Incidents.Where(p=>p.Value.Scope==scope).Select(p=>p.Key).ToArray()) Incidents.Remove(key);
            if(Incidents.Count==0) _acknowledged=false;
        }
        Dispatcher.UIThread.Post(ScheduleRefresh);
    }
    private static void ScheduleRefresh()
    {
        if (_mergeTimer is null) {
            _mergeTimer=new DispatcherTimer { Interval=TimeSpan.FromMilliseconds(600) };
            _mergeTimer.Tick+=(_,_)=> { _mergeTimer.Stop();Refresh(); };
        }
        // Keep the deadline: continuous sampling cannot postpone the window forever.
        if(!_mergeTimer.IsEnabled) _mergeTimer.Start();
    }
    private static void Refresh()
    {
        Incident[] current; bool ready,ack;
        lock(Gate) {
            current=Incidents.Values.ToArray();ack=_acknowledged;
            ready=current.Any(i=>Environment.TickCount64-i.Since>=5000);
        }
        if(current.Length==0) { _dialog?.Close();return; }
        if(_dialog is null && (!ready || ack)) return;
        string body=string.Join("\n\n",current.GroupBy(i=>i.Name).OrderBy(g=>g.Key)
            .Select(g=>g.Key+"\n"+string.Join("\n",g.SelectMany(i=>i.Keys).Distinct().OrderBy(k=>k))));
        if(_dialog is not null) { _details!.Text=body;return; }
        var close=new Button { Content=LocalizationManager.Text("知道了","OK"),HorizontalAlignment=HorizontalAlignment.Right };
        _details=new TextBlock { Text=body,TextWrapping=Avalonia.Media.TextWrapping.Wrap };
        var panel=new StackPanel { Margin=new Avalonia.Thickness(22),Spacing=16 };
        panel.Children.Add(new TextBlock { Text=LocalizationManager.Text("以下 HUD 方案的数据不可用或无效：","Data is unavailable or invalid for these HUD profiles:"),FontSize=16,TextWrapping=Avalonia.Media.TextWrapping.Wrap });
        panel.Children.Add(new ScrollViewer { Content=_details,MaxHeight=460,VerticalScrollBarVisibility=Avalonia.Controls.Primitives.ScrollBarVisibility.Auto });
        panel.Children.Add(new TextBlock { Text=LocalizationManager.Text("所有实例的无效变量合并在此窗口；新增故障会在这里更新。请检查权限、设备支持和数据源。关闭后，同一故障期间不会逐个重复弹窗。","Unavailable variables from all instances appear here; new failures update this window. Check permissions, device support and data sources. Closing acknowledges this failure episode without opening separate dialogs."),TextWrapping=Avalonia.Media.TextWrapping.Wrap });
        panel.Children.Add(close);
        var dialog=new Window { Title=LocalizationManager.Text("HUD 数据提示","HUD data warning"),Width=620,SizeToContent=SizeToContent.Height,MaxHeight=720,Content=panel,CanResize=true,Topmost=true,WindowStartupLocation=WindowStartupLocation.CenterScreen };
        _dialog=dialog;close.Click+=(_,_)=>dialog.Close();
        dialog.Closed+=(_,_)=> { _dialog=null;_details=null;lock(Gate) _acknowledged=Incidents.Count>0; };
        dialog.Show();
    }
}
