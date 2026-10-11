using System.Runtime.InteropServices;
using Avalonia;

namespace EndfieldChargePlus.Interop;

/// <summary>X11/XWayland integration; native Wayland does not permit global pointer queries.</summary>
internal static class LinuxDesktop
{
    private static readonly object Gate = new();
    private static IntPtr _display;

    internal static void PrepareClipboard()
    {
        if (!OperatingSystem.IsLinux()) return;
        lock (Gate)
        {
            try
            {
                // Avalonia 11.2 looks up existing clipboard atoms. A fresh X server or
                // minimal desktop may have no clipboard manager to create them first.
                XInitThreads();
                if (!Connect()) return;
                foreach (var atom in new[] { "CLIPBOARD", "TARGETS", "UTF8_STRING", "UTF16_STRING", "MULTIPLE", "ATOM_PAIR", "SAVE_TARGETS" })
                    XInternAtom(_display, atom, false);
                XFlush(_display);
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
        }
    }

    private static bool Connect()
    {
        if (!OperatingSystem.IsLinux()) return false;
        if (_display == IntPtr.Zero) _display = XOpenDisplay(IntPtr.Zero);
        return _display != IntPtr.Zero;
    }

    internal static bool TryGetCursorPosition(out PixelPoint point)
    {
        point = default;
        lock (Gate)
        {
            try
            {
                if (!Connect()) return false;
                if (XQueryPointer(_display, XDefaultRootWindow(_display), out _, out _, out int x, out int y,
                        out _, out _, out _) == 0) return false;
                point = new PixelPoint(x, y);
                return true;
            }
            catch (DllNotFoundException) { return false; }
            catch (EntryPointNotFoundException) { return false; }
        }
    }

    internal static void SetClickThrough(IntPtr window)
    {
        lock (Gate)
        {
            try
            {
                if (window == IntPtr.Zero || !Connect() || XShapeQueryExtension(_display, out _, out _) == 0) return;
                HudRegions.Remove(window);
                // An empty ShapeInput region lets mouse events reach windows underneath the HUD.
                XShapeCombineRectangles(_display, window, 2, 0, 0, IntPtr.Zero, 0, 0, 0);
                XFlush(_display);
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
        }
    }

    [StructLayout(LayoutKind.Sequential)] private struct XRectangle { public short X,Y; public ushort Width,Height; }
    private static readonly Dictionary<IntPtr,(int X,int Y,int W,int H)> HudRegions=new();
    internal static void ForgetHud(IntPtr window) { lock(Gate) HudRegions.Remove(window); }
    internal static void SetHudInputRegion(IntPtr window,int x,int y,int width,int height)
    {
        lock (Gate)
        {
            if (!Connect() || XShapeQueryExtension(_display,out _,out _)==0) return;
            var key=(x,y,width,height);
            if(HudRegions.TryGetValue(window,out var previous) && previous==key) return;
            HudRegions[window]=key;
            width=Math.Clamp(width,1,65535);height=Math.Clamp(height,1,65535);
            double radius=Math.Min(width,height)/2d;
            var rows=new List<XRectangle>();
            for(int row=0;row<height;row++) {
                double dy=Math.Abs(row+.5-height/2d);
                int inset=dy>=radius ? (int)radius : (int)Math.Ceiling(radius-Math.Sqrt(radius*radius-dy*dy));
                int w=Math.Max(1,width-2*inset);
                if(rows.Count>0 && rows[^1].X==x+inset && rows[^1].Width==w) {
                    var prev=rows[^1];prev.Height++;rows[^1]=prev;
                } else rows.Add(new XRectangle { X=(short)(x+inset),Y=(short)(y+row),Width=(ushort)w,Height=1 });
            }
            var memory=Marshal.AllocHGlobal(rows.Count*Marshal.SizeOf<XRectangle>());
            try {
                for(int i=0;i<rows.Count;i++) Marshal.StructureToPtr(rows[i],memory+i*Marshal.SizeOf<XRectangle>(),false);
                XShapeCombineRectangles(_display,window,2,0,0,memory,rows.Count,0,0);XFlush(_display);
            } finally { Marshal.FreeHGlobal(memory); }
        }
    }

    internal static void Close()
    {
        lock (Gate)
        {
            if (_display != IntPtr.Zero) XCloseDisplay(_display);
            _display = IntPtr.Zero;
        }
    }

    [DllImport("libX11.so.6")] private static extern IntPtr XOpenDisplay(IntPtr name);
    [DllImport("libX11.so.6")] private static extern int XInitThreads();
    [DllImport("libX11.so.6")] private static extern IntPtr XInternAtom(IntPtr display, string name, bool onlyIfExists);
    [DllImport("libX11.so.6")] private static extern int XCloseDisplay(IntPtr display);
    [DllImport("libX11.so.6")] private static extern IntPtr XDefaultRootWindow(IntPtr display);
    [DllImport("libX11.so.6")] private static extern int XFlush(IntPtr display);
    [DllImport("libX11.so.6")] private static extern int XQueryPointer(IntPtr display, IntPtr window,
        out IntPtr root, out IntPtr child, out int rootX, out int rootY, out int winX, out int winY, out uint mask);
    [DllImport("libXext.so.6")] private static extern int XShapeQueryExtension(IntPtr display, out int eventBase, out int errorBase);
    [DllImport("libXext.so.6")] private static extern void XShapeCombineRectangles(IntPtr display, IntPtr window,
        int kind, int x, int y, IntPtr rectangles, int count, int operation, int ordering);
}
