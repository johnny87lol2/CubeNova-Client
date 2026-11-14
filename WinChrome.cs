using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CubeNova;

/// <summary>讓視窗標題列變成深色 (Windows 10 20H1+ / Windows 11)</summary>
public static class WinChrome
{
    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    [DllImport("user32.dll")] static extern bool ReleaseCapture();
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flags);
    [DllImport("user32.dll")] static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO mi);

    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    struct MONITORINFO { public int cbSize; public RECT rcMonitor; public RECT rcWork; public int dwFlags; }

    const int WM_NCLBUTTONDOWN = 0x00A1;

    /// <summary>開始系統的拖曳視窗 (如同按住標題列)</summary>
    public static void BeginMove(Window w) => Begin(w, 2 /* HTCAPTION */);

    /// <summary>開始系統的縮放視窗。edge: l r t b tl tr bl br</summary>
    public static void BeginResize(Window w, string edge)
    {
        int ht = edge switch
        {
            "l" => 10, "r" => 11, "t" => 12, "tl" => 13, "tr" => 14,
            "b" => 15, "bl" => 16, "br" => 17, _ => 0,
        };
        if (ht != 0) Begin(w, ht);
    }

    static void Begin(Window w, int hitTest)
    {
        var h = new WindowInteropHelper(w).Handle;
        ReleaseCapture();
        PostMessage(h, WM_NCLBUTTONDOWN, (IntPtr)hitTest, IntPtr.Zero);
    }

    /// <summary>視窗所在螢幕的工作區 (不含工作列)，單位為 WPF 的 DIP</summary>
    public static Rect WorkArea(Window w)
    {
        var h = new WindowInteropHelper(w).Handle;
        var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(MonitorFromWindow(h, 2), ref mi))
            return SystemParameters.WorkArea;
        var src = PresentationSource.FromVisual(w);
        if (src?.CompositionTarget == null) return SystemParameters.WorkArea;
        var m = src.CompositionTarget.TransformFromDevice;
        var tl = m.Transform(new Point(mi.rcWork.Left, mi.rcWork.Top));
        var br = m.Transform(new Point(mi.rcWork.Right, mi.rcWork.Bottom));
        return new Rect(tl, br);
    }

    public static void Dark(Window w)
    {
        try
        {
            var h = new WindowInteropHelper(w).Handle;
            int dark = 1;
            DwmSetWindowAttribute(h, 20, ref dark, 4);          // 深色模式標題列
            int caption = 0x120D0B;                              // COLORREF 0x00BBGGRR = #0B0D12
            DwmSetWindowAttribute(h, 35, ref caption, 4);       // 標題列顏色 (Windows 11)
            int border = 0x120D0B;
            DwmSetWindowAttribute(h, 34, ref border, 4);        // 邊框顏色 (Windows 11)
            int text = 0xF6F1EE;                                 // #EEF1F6
            DwmSetWindowAttribute(h, 36, ref text, 4);          // 標題文字顏色 (Windows 11)
        }
        catch { }
    }
}
