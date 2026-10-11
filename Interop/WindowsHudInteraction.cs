using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Threading;
namespace EndfieldChargePlus.Interop;

internal sealed class WindowsHudInteraction : IDisposable
{
    private delegate IntPtr SubclassProc(IntPtr hwnd,uint message,IntPtr wparam,IntPtr lparam,UIntPtr id,IntPtr data);
    private readonly IntPtr _handle;
    private readonly Func<PixelPoint,bool> _contains;
    private readonly Action _clicked;
    private readonly SubclassProc _procedure;
    private readonly DispatcherTimer _pointerTimer;
    private bool _enabled;
    private bool _attached;
    private const int Style=-20;
    private const long Transparent=0x20;
    private static readonly UIntPtr Id=new(0xEC2026);
    internal WindowsHudInteraction(IntPtr handle,Func<PixelPoint,bool> contains,Action clicked)
    {
        _handle=handle; _contains=contains; _clicked=clicked; _procedure=Procedure;
        _attached=SetWindowSubclass(handle,_procedure,Id,IntPtr.Zero);
        _pointerTimer=new DispatcherTimer { Interval=TimeSpan.FromMilliseconds(16) };
        _pointerTimer.Tick+=(_,_) => Refresh();
    }
    internal void SetEnabled(bool enabled)
    {
        _enabled=enabled && _attached;
        if (_enabled) _pointerTimer.Start(); else _pointerTimer.Stop();
        Refresh();
    }
    private void Refresh()
    {
        bool inside=_enabled && GetCursorPos(out var point) && _contains(point);
        long style=IntPtr.Size==8 ? GetWindowLongPtr(_handle,Style).ToInt64() : GetWindowLong(_handle,Style);
        long next=inside ? style & ~Transparent : style | Transparent;
        if (style==next) return;
        if (IntPtr.Size==8) SetWindowLongPtr(_handle,Style,new IntPtr(next)); else SetWindowLong(_handle,Style,unchecked((int)next));
    }
    private IntPtr Procedure(IntPtr hwnd,uint message,IntPtr wparam,IntPtr lparam,UIntPtr id,IntPtr data)
    {
        if (_enabled && message==0x84) // WM_NCHITTEST: only the visible capsule accepts a click.
            return GetCursorPos(out var point) && _contains(point) ? new IntPtr(1) : new IntPtr(-1);
        if (_enabled && message==0x202 && GetCursorPos(out var cursor) && _contains(cursor))
        { Dispatcher.UIThread.Post(_clicked); return IntPtr.Zero; }
        return DefSubclassProc(hwnd,message,wparam,lparam);
    }
    public void Dispose()
    {
        SetEnabled(false);
        if (_attached) RemoveWindowSubclass(_handle,_procedure,Id);
        _attached=false;
    }
    [DllImport("comctl32.dll")] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool SetWindowSubclass(IntPtr h,SubclassProc p,UIntPtr id,IntPtr data);
    [DllImport("comctl32.dll")] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool RemoveWindowSubclass(IntPtr h,SubclassProc p,UIntPtr id);
    [DllImport("comctl32.dll")] private static extern IntPtr DefSubclassProc(IntPtr h,uint msg,IntPtr w,IntPtr l);
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool GetCursorPos(out PixelPoint p);
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr h,int i);
    [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")] private static extern IntPtr SetWindowLongPtr(IntPtr h,int i,IntPtr v);
    [DllImport("user32.dll",EntryPoint="GetWindowLongW")] private static extern int GetWindowLong(IntPtr h,int i);
    [DllImport("user32.dll",EntryPoint="SetWindowLongW")] private static extern int SetWindowLong(IntPtr h,int i,int v);
}
