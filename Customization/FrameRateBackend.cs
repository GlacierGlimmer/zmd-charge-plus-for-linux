using System.Runtime.InteropServices;
using System.Text;

namespace EndfieldChargePlus.Customization;

/// <summary>X11 Present completion notifications. Damage/paint operations and refresh-rate guesses are never counted.</summary>
public static class FrameRateBackend
{
    private static readonly object Gate=new();
    private static IntPtr _display;
    private static int _opcode;
    private static string _status="等待 Present 帧事件 / Waiting for Present frames";
    private static readonly Dictionary<ulong,FrameEventWindow> Frames=new();
    private static readonly Dictionary<ulong,ulong> Owners=new();
    private static readonly Dictionary<ulong,ulong> Subscriptions=new();
    private static readonly Dictionary<string,FrameEventWindow> DisplayFrames=new();
    private static readonly Dictionary<string,ulong> LastMsc=new();
    private static long _nextScan;
    private static long _lastDemand;
    private static Avalonia.PixelRect[] _screenBounds=Array.Empty<Avalonia.PixelRect>();
    private delegate int ErrorHandler(IntPtr display,IntPtr error);
    private static readonly ErrorHandler OnError=HandleXError;
    private static IntPtr _previousError;
    private static unsafe int HandleXError(IntPtr display,IntPtr error) { if(display==_display || _previousError==IntPtr.Zero) return 0; return ((delegate* unmanaged[Cdecl]<IntPtr,IntPtr,int>)_previousError)(display,error); }


    private static bool Connect()
    {
        if (!OperatingSystem.IsLinux()) return false;
        try
        {
            if (_display!=IntPtr.Zero) return true;
            XInitThreads(); _display=XOpenDisplay(IntPtr.Zero);
            if (_display==IntPtr.Zero) { _status="需要 X11／XWayland 会话 / An X11/XWayland session is required"; return false; }
            _previousError=XSetErrorHandler(Marshal.GetFunctionPointerForDelegate(OnError));
            int major=1, minor=2;
            if (XPresentQueryExtension(_display,out _opcode,out _,out _)==0 || XPresentQueryVersion(_display,ref major,ref minor)==0)
            { XCloseDisplay(_display); _display=IntPtr.Zero; XSetErrorHandler(_previousError); _status="X Server 不支持 Present 扩展 / X server lacks Present"; return false; }
            _=Task.Run(async () => {
                while (true) { await Task.Delay(10); lock(Gate) { if (_display==IntPtr.Zero) return; if (Environment.TickCount64-_lastDemand>6000) { Stop(); return; } Pump(); } }
            });
            return true;
        }
        catch(DllNotFoundException) { Stop(); _status="需要 libXpresent.so.1 / Install libxpresent1"; return false; }
        catch(EntryPointNotFoundException) { Stop(); _status="Present 采集接口不可用 / Present API unavailable"; return false; }
    }
    public static IReadOnlyList<FrameTarget> Windows()
    {
        lock(Gate)
        {
            _lastDemand=Environment.TickCount64;
            if (!Connect()) return Array.Empty<FrameTarget>();
            var root=XDefaultRootWindow(_display);
            var list=Property(root,"_NET_CLIENT_LIST_STACKING");
            var result=new List<FrameTarget>();
            for(int i=0;i<list.Length/IntPtr.Size;i++)
            {
                ulong id=IntPtr.Size==8 ? BitConverter.ToUInt64(list,i*8) : BitConverter.ToUInt32(list,i*4);
                string title=Encoding.UTF8.GetString(Property(id,"_NET_WM_NAME")).TrimEnd('\0');
                var pidData=Property(id,"_NET_WM_PID");
                if (pidData.Length>=4 && BitConverter.ToUInt32(pidData)==Environment.ProcessId) continue;
                if (!string.IsNullOrWhiteSpace(title)) result.Add(new(id.ToString("x"),title));
            }
            return result.OrderBy(w => w.Name).ToArray();
        }
    }
    public static IReadOnlyList<FrameTarget> Displays()
    {
        var screens=Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.Windows.FirstOrDefault()?.Screens.All : null;
        _screenBounds=screens?.Select(s=>s.Bounds).ToArray() ?? Array.Empty<Avalonia.PixelRect>();
        return screens?.Select((s,i)=>new FrameTarget($"screen:{i}",$"显示器 {i+1} · {s.Bounds.Width}×{s.Bounds.Height}")).ToArray() ?? Array.Empty<FrameTarget>();
    }
    public static FrameReading Read(string kind,string target)
    {
        lock(Gate)
        {
            _lastDemand=Environment.TickCount64;
            if (!Connect()) return new(null,target,_status);
            var windows=Windows();
            if (Environment.TickCount64>=_nextScan)
            {
                foreach(var window in windows) if (ulong.TryParse(window.Id,System.Globalization.NumberStyles.HexNumber,null,out var id)) Subscribe(id,id,0);
                XFlush(_display); _nextScan=Environment.TickCount64+3000;
            }
            if (kind=="window")
            {
                var selected=windows.FirstOrDefault(w => w.Id==target);
                if (selected is null) return new(null,target,"请选择正在运行的窗口 / Select a running window");
                ulong id=Convert.ToUInt64(target,16);
                return new(Frames.TryGetValue(id,out var events) ? events.Read(Environment.TickCount64) : null,selected.Name,_status);
            }
            var displays=Displays(); var display=string.IsNullOrEmpty(target) ? displays.FirstOrDefault() : displays.FirstOrDefault(d=>d.Id==target);
            if (display is null) return new(null,target,"显示器已断开 / Display disconnected");
            return new(DisplayFrames.TryGetValue(display.Id,out var frames) ? frames.Read(Environment.TickCount64) : null,display.Name,_status);
        }
    }
    private static void Subscribe(ulong window,ulong owner,int depth)
    {
        if (!Subscriptions.ContainsKey(window)) { Subscriptions[window]=XPresentSelectInput(_display,window,2); Owners[window]=owner; }
        if (depth>=3 || XQueryTree(_display,window,out _,out _,out var children,out uint count)==0) return;
        try { for(int i=0;i<count;i++) Subscribe(unchecked((ulong)Marshal.ReadIntPtr(children,i*IntPtr.Size).ToInt64()),owner,depth+1); }
        finally { if(children!=IntPtr.Zero) XFree(children); }
    }
    private static void Pump()
    {
        IntPtr storage=Marshal.AllocHGlobal(24*IntPtr.Size);
        try
        {
            for(int i=0;i<4096 && XPending(_display)>0;i++)
            {
                XNextEvent(_display,storage); var cookie=Marshal.PtrToStructure<EventCookie>(storage);
                if (cookie.Type!=35 || cookie.Extension!=_opcode || cookie.EventType!=1 || XGetEventData(_display,storage)==0) continue;
                try
                {
                    cookie=Marshal.PtrToStructure<EventCookie>(storage); var complete=Marshal.PtrToStructure<CompleteEvent>(cookie.Data);
                    if(complete.Kind!=0 || complete.Mode==2 || !Owners.TryGetValue(complete.Window,out var owner)) continue;
                    if (!Frames.TryGetValue(owner,out var events)) Frames[owner]=events=new();
                    long now=Environment.TickCount64; events.Add(complete.Ust/1e6,now);
                    if(XGetGeometry(_display,owner,out var root,out _,out _,out uint width,out uint height,out _,out _)!=0 &&
                        XTranslateCoordinates(_display,owner,root,0,0,out int x,out int y,out _)!=0)
                    {
                        var screens=_screenBounds;
                        for(int index=0;index<screens.Length;index++)
                            if(screens[index].Contains(new Avalonia.PixelPoint(x+(int)width/2,y+(int)height/2)))
                            {
                                string id=$"screen:{index}"; if(!DisplayFrames.TryGetValue(id,out var display)) DisplayFrames[id]=display=new();
                                // Multiple windows can complete on the same scanout. Count the display frame once.
                                if (!LastMsc.TryGetValue(id,out var msc) || msc!=complete.Msc) { display.Add(complete.Ust/1e6,now); LastMsc[id]=complete.Msc; }
                            }
                    }
                    _status="OK";
                }
                finally { XFreeEventData(_display,storage); }
            }
        }
        finally { Marshal.FreeHGlobal(storage); }
    }
    private static byte[] Property(ulong window,string name)
    {
        ulong atom=XInternAtom(_display,name,true); if(atom==0) return Array.Empty<byte>();
        if(XGetWindowProperty(_display,window,atom,0,65536,false,0,out _,out int format,out ulong count,out _,out var pointer)!=0 || pointer==IntPtr.Zero) return Array.Empty<byte>();
        try { int size=checked((int)Math.Min(count,65536))*(format==32 ? IntPtr.Size : Math.Max(1,format/8)); var bytes=new byte[size]; Marshal.Copy(pointer,bytes,0,size); return bytes; }
        finally { XFree(pointer); }
    }
    public static void KeepAlive(string kind,string target) { lock(Gate) _lastDemand=Environment.TickCount64; }
    public static void Stop() { lock(Gate) { if(_display!=IntPtr.Zero) { XCloseDisplay(_display); XSetErrorHandler(_previousError); } _display=IntPtr.Zero; Frames.Clear(); Owners.Clear(); Subscriptions.Clear(); DisplayFrames.Clear(); LastMsc.Clear(); _nextScan=0; } }
    [StructLayout(LayoutKind.Sequential)] private struct EventCookie { public int Type; public ulong Serial; public int SendEvent; public IntPtr Display; public int Extension,EventType; public uint Cookie; public IntPtr Data; }
    [StructLayout(LayoutKind.Sequential)] private struct CompleteEvent { public int Type; public ulong Serial; public int SendEvent; public IntPtr Display; public int Extension,EventType; public ulong EventId,Window; public uint SerialNumber; public ulong Ust,Msc; public byte Kind,Mode; }
    [DllImport("libX11.so.6")] private static extern int XInitThreads();
    [DllImport("libX11.so.6")] private static extern IntPtr XSetErrorHandler(IntPtr handler);
    [DllImport("libX11.so.6")] private static extern IntPtr XOpenDisplay(IntPtr name);
    [DllImport("libX11.so.6")] private static extern int XCloseDisplay(IntPtr display);
    [DllImport("libX11.so.6")] private static extern ulong XDefaultRootWindow(IntPtr display);
    [DllImport("libX11.so.6")] private static extern ulong XInternAtom(IntPtr display,string name,bool onlyIfExists);
    [DllImport("libX11.so.6")] private static extern int XGetWindowProperty(IntPtr display,ulong window,ulong property,long offset,long length,bool delete,ulong requested,out ulong type,out int format,out ulong count,out ulong remaining,out IntPtr pointer);
    [DllImport("libX11.so.6")] private static extern int XFree(IntPtr pointer);
    [DllImport("libX11.so.6")] private static extern int XQueryTree(IntPtr display,ulong window,out ulong root,out ulong parent,out IntPtr children,out uint count);
    [DllImport("libX11.so.6")] private static extern int XFlush(IntPtr display);
    [DllImport("libX11.so.6")] private static extern int XPending(IntPtr display);
    [DllImport("libX11.so.6")] private static extern int XNextEvent(IntPtr display,IntPtr result);
    [DllImport("libX11.so.6")] private static extern int XGetEventData(IntPtr display,IntPtr cookie);
    [DllImport("libX11.so.6")] private static extern void XFreeEventData(IntPtr display,IntPtr cookie);
    [DllImport("libX11.so.6")] private static extern int XGetGeometry(IntPtr display,ulong drawable,out ulong root,out int x,out int y,out uint width,out uint height,out uint border,out uint depth);
    [DllImport("libX11.so.6")] private static extern int XTranslateCoordinates(IntPtr display,ulong source,ulong destination,int x,int y,out int targetX,out int targetY,out ulong child);
    [DllImport("libXpresent.so.1")] private static extern int XPresentQueryExtension(IntPtr display,out int opcode,out int eventBase,out int errorBase);
    [DllImport("libXpresent.so.1")] private static extern int XPresentQueryVersion(IntPtr display,ref int major,ref int minor);
    [DllImport("libXpresent.so.1")] private static extern ulong XPresentSelectInput(IntPtr display,ulong window,uint events);
}
