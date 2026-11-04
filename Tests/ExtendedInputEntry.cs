using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Threading;
using EndfieldChargePlus;
using EndfieldChargePlus.Interop;
internal static class ExtendedInput
{
    static int checks;
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
    static IntPtr display;
    static int Main(){
        if(Environment.GetEnvironmentVariable("ECP_INPUT_TEST_ISOLATED")!="1" || !(Environment.GetEnvironmentVariable("XAUTHORITY")?.Contains("xvfb-run")==true) || !(Environment.GetEnvironmentVariable("DISPLAY")?.StartsWith(":")==true))throw new Exception("Use an isolated Xvfb display.");
        AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();display=XOpenDisplay(IntPtr.Zero);Exception? error=null;bool completed=false;using var done=new CancellationTokenSource(TimeSpan.FromSeconds(45));
        Dispatcher.UIThread.Post(async()=>{try{await Run();completed=true;}catch(Exception ex){error=ex;}finally{done.Cancel();}});Dispatcher.UIThread.MainLoop(done.Token);XCloseDisplay(display);if(error is not null)throw error;if(!completed)throw new TimeoutException("Native input checks did not finish.");Console.WriteLine($"PASS {checks} native X11 extended-key/mouse checks");return 0;
    }
    static async Task Wait(Func<bool> ready,string text){var end=DateTime.UtcNow.AddSeconds(2);while(!ready() && DateTime.UtcNow<end)await Task.Delay(10);Check(ready(),text);}
    static byte Key(string name)=>XKeysymToKeycode(display,XStringToKeysym(name));
    static void Press(HudHotkeyGesture chord){
        if((chord.Modifiers&2)!=0)XTestFakeKeyEvent(display,Key("Control_L"),true,0);if((chord.Modifiers&1)!=0)XTestFakeKeyEvent(display,Key("Alt_L"),true,0);
        if(chord.IsMouse){XTestFakeMotionEvent(display,-1,200,200,0);uint button=chord.MouseButton switch{1=>1,2=>3,3=>2,4=>8,5=>9,_=>0};XTestFakeButtonEvent(display,button,true,0);XTestFakeButtonEvent(display,button,false,0);}
        else{byte key=Key(chord.X11KeyName);XTestFakeKeyEvent(display,key,true,0);XTestFakeKeyEvent(display,key,false,0);}
        if((chord.Modifiers&1)!=0)XTestFakeKeyEvent(display,Key("Alt_L"),false,0);if((chord.Modifiers&2)!=0)XTestFakeKeyEvent(display,Key("Control_L"),false,0);XFlush(display);
    }
    static async Task Run(){
        foreach(string text in new[]{"Up","Down","Left","Right","Insert","Tab","Backspace","Ctrl+Alt+M","F12","NumPad0","OemPlus","Mouse1","Mouse2","Mouse3","Mouse4","Mouse5","Ctrl+Mouse3","Ctrl+Mouse5"}){
            int count=0;using var binding=new DesktopGlobalHotkey(()=>count++);Check(binding.TryUpdate(text),$"register {text}, error {binding.LastError}");HudHotkeyGesture.TryParse(text,out var chord);Press(chord);await Wait(()=>count==1,"dispatch once "+text);await Task.Delay(35);Check(count==1,"no duplicate "+text);Press(chord);await Wait(()=>count==2,"released binding can fire again "+text);
        }
        int ring=0,shortcut=0;using var radial=new UnixRadialInput(_=>true,(kind,_,_)=>{if(kind==0)ring++;});
        using(var binding=new DesktopGlobalHotkey(()=>shortcut++)){Check(binding.TryUpdate("Mouse3"),"middle shortcut coexists with radial input connection");HudHotkeyGesture.TryParse("Mouse3",out var chord);Press(chord);await Wait(()=>shortcut==1,"middle shortcut has explicit precedence");Check(ring==0,"middle does not also open radial menu");}
        HudHotkeyGesture.TryParse("Mouse3",out var middle);Press(middle);await Wait(()=>ring==1,"clearing shortcut restores radial opening");
    }
    [DllImport("libX11.so.6")]static extern IntPtr XOpenDisplay(IntPtr name);
    [DllImport("libX11.so.6")]static extern int XCloseDisplay(IntPtr display);
    [DllImport("libX11.so.6")]static extern UIntPtr XStringToKeysym(string name);
    [DllImport("libX11.so.6")]static extern byte XKeysymToKeycode(IntPtr display,UIntPtr symbol);
    [DllImport("libX11.so.6")]static extern int XFlush(IntPtr display);
    [DllImport("libXtst.so.6")]static extern int XTestFakeMotionEvent(IntPtr display,int screen,int x,int y,ulong delay);
    [DllImport("libXtst.so.6")]static extern int XTestFakeKeyEvent(IntPtr display,uint key,bool press,ulong delay);
    [DllImport("libXtst.so.6")]static extern int XTestFakeButtonEvent(IntPtr display,uint button,bool press,ulong delay);
}
