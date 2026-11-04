using System.Runtime.InteropServices;
using Avalonia;
using EndfieldChargePlus.Diagnostics;

namespace EndfieldChargePlus.Interop;

// A single lightweight hook only claims middle-button gestures beginning on a HUD.
// The visual HUD remains mouse-through; left/right buttons are never intercepted.
internal sealed class WindowsRadialInput : IDisposable
{
    private delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);
    private static readonly HookProc Root=Process;
    private static readonly List<WindowsRadialInput> Entries=new();
    private static WindowsRadialInput? _active;
    private static IntPtr _hook;
    private readonly Func<PixelPoint,bool> _hit;
    private readonly Action<int,PixelPoint,int> _input;
    internal Func<bool>? WantsWheel;
    internal WindowsRadialInput(Func<PixelPoint,bool> hit,Action<int,PixelPoint,int> input)
    {
        _hit=hit;_input=input; Entries.Add(this);
        if(_hook==IntPtr.Zero) {
            _hook=SetWindowsHookEx(14,Root,GetModuleHandle(null),0);
            if(_hook==IntPtr.Zero) AppLog.Error("Unable to register middle-button HUD gesture.",new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError()));
        }
    }
    private static IntPtr Process(int code,IntPtr message,IntPtr data)
    {
        if(code<0) return CallNextHookEx(_hook,code,message,data);
        try {
            int msg=message.ToInt32();var value=Marshal.PtrToStructure<MouseData>(data);var point=new PixelPoint(value.X,value.Y);
            if (ProcessMessage(msg,point,(short)(value.MouseDataValue>>16))) return new(1);
        } catch(Exception ex) { AppLog.Error("HUD gesture input failed.",ex); }
        return CallNextHookEx(_hook,code,message,data);
    }
    // Use the intercepted down/up stream as the source of truth. A suppressed
    // WM_MBUTTONDOWN need not appear in GetAsyncKeyState, so polling it cancels
    // a valid hold before its timer opens the ring.
    internal static bool ProcessMessage(int msg, PixelPoint point, int wheel)
    {
            if(msg==0x207 && _active is null) {
                _active=Entries.LastOrDefault(e=>e._hit(point));
                if(_active is not null) { _active._input(0,point,0);return true; }
            } else if(_active is {} active) {
                if(msg==0x200) active._input(1,point,0);
                else if(msg==0x208) { _active=null;active._input(2,point,0);return true; }
                else if(msg==0x20A && active.WantsWheel?.Invoke()==true) { active._input(3,point,wheel);return true; }
            }
        return false;
    }
    public void Dispose() {
        Entries.Remove(this);if(_active==this)_active=null;
        if(Entries.Count==0 && _hook!=IntPtr.Zero) {UnhookWindowsHookEx(_hook);_hook=IntPtr.Zero;}
    }
    internal static bool EscapePressed=>OperatingSystem.IsWindows() && (GetAsyncKeyState(0x1B)&0x8000)!=0;
    internal static IntPtr Foreground=>OperatingSystem.IsWindows() ? GetForegroundWindow() : IntPtr.Zero;
    [StructLayout(LayoutKind.Sequential)] private struct MouseData {public int X,Y;public uint MouseDataValue,Flags,Time;public UIntPtr ExtraInfo;}
    [DllImport("user32.dll",SetLastError=true)] private static extern IntPtr SetWindowsHookEx(int type,HookProc proc,IntPtr module,uint thread);
    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr message,IntPtr data);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string? module);
}
