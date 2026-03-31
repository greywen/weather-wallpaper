using System.Runtime.InteropServices;
using System.Windows;

namespace WeatherWallpaper.Native;

/// <summary>
/// Core desktop integration - manages WorkerW/Progman window hierarchy
/// to place wallpaper behind desktop icons (based on lively's WinDesktopCore).
/// </summary>
internal sealed class DesktopWorker
{
    private IntPtr _progman;
    private IntPtr _workerW;
    private IntPtr _shellDLL_DefView;
    private bool _isRaisedDesktop;

    /// <summary>
    /// Initialize the desktop layer by finding/creating WorkerW.
    /// </summary>
    public bool Initialize()
    {
        // Find Program Manager
        _progman = NativeMethods.FindWindow("Progman", null);
        if (_progman == IntPtr.Zero)
            return false;

        // Check for raised desktop (Windows 11 / newer Windows 10)
        var exStyle = NativeMethods.GetWindowLongPtr(_progman, (int)NativeMethods.GWL.GWL_EXSTYLE).ToInt64();
        _isRaisedDesktop = (exStyle & NativeMethods.WS_EX_NOREDIRECTIONBITMAP) != 0;

        // Send 0x052C to Progman to spawn WorkerW behind desktop icons
        NativeMethods.SendMessageTimeout(
            _progman,
            0x052C,
            new IntPtr(0xD),
            new IntPtr(0x1),
            NativeMethods.SendMessageTimeoutFlags.SMTO_NORMAL,
            1000,
            out _);

        // Find SHELLDLL_DefView and the WorkerW behind it
        _shellDLL_DefView = IntPtr.Zero;
        _workerW = IntPtr.Zero;

        NativeMethods.EnumWindows((tophandle, topparamhandle) =>
        {
            IntPtr p = NativeMethods.FindWindowEx(tophandle, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (p != IntPtr.Zero)
            {
                _workerW = NativeMethods.FindWindowEx(IntPtr.Zero, tophandle, "WorkerW", null);
                _shellDLL_DefView = p;
            }
            return true;
        }, IntPtr.Zero);

        if (_isRaisedDesktop)
        {
            // On raised desktop, WorkerW is a child of Progman
            _workerW = NativeMethods.FindWindowEx(_progman, IntPtr.Zero, "WorkerW", null);
        }

        return _workerW != IntPtr.Zero || _progman != IntPtr.Zero;
    }

    /// <summary>
    /// Attach a window handle to the desktop as wallpaper for a specific monitor.
    /// </summary>
    public bool SetWallpaper(IntPtr hwnd, Rect monitorBounds)
    {
        if (_workerW == IntPtr.Zero && _progman == IntPtr.Zero)
            return false;

        int x = (int)monitorBounds.X;
        int y = (int)monitorBounds.Y;
        int w = (int)monitorBounds.Width;
        int h = (int)monitorBounds.Height;

        // Position the window to the monitor
        NativeMethods.SetWindowPos(hwnd, new IntPtr(1), x, y, w, h, NativeMethods.SWP_NOACTIVATE);

        var prct = new NativeMethods.RECT();
        if (!_isRaisedDesktop && _workerW != IntPtr.Zero)
        {
            NativeMethods.MapWindowPoints(hwnd, _workerW, ref prct, 2);
        }

        // Attach to desktop
        if (!TryAttachToDesktop(hwnd))
            return false;

        if (_isRaisedDesktop)
        {
            // On raised desktop, position relative to progman
            NativeMethods.SetWindowPos(hwnd, new IntPtr(1), x, y, w, h,
                NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_NOZORDER);
        }
        else
        {
            // Position relative to WorkerW
            NativeMethods.SetWindowPos(hwnd, new IntPtr(1), prct.Left, prct.Top, w, h,
                NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_NOZORDER);
        }

        return true;
    }

    private bool TryAttachToDesktop(IntPtr hwnd)
    {
        if (_isRaisedDesktop)
        {
            // Set WS_CHILD style
            var style = NativeMethods.GetWindowLongPtr(hwnd, (int)NativeMethods.GWL.GWL_STYLE).ToInt64();
            style |= NativeMethods.WS_CHILD;
            NativeMethods.SetWindowLongPtr(new HandleRef(null, hwnd), (int)NativeMethods.GWL.GWL_STYLE, new IntPtr(style));

            // Add WS_EX_LAYERED and set full opacity
            var exStyle = NativeMethods.GetWindowLongPtr(hwnd, (int)NativeMethods.GWL.GWL_EXSTYLE).ToInt64();
            if ((exStyle & NativeMethods.WS_EX_LAYERED) == 0)
            {
                exStyle |= NativeMethods.WS_EX_LAYERED;
                NativeMethods.SetWindowLongPtr(new HandleRef(null, hwnd), (int)NativeMethods.GWL.GWL_EXSTYLE, new IntPtr(exStyle));
            }
            NativeMethods.SetLayeredWindowAttributes(hwnd, 0, 255, NativeMethods.LWA_ALPHA);

            // Set parent to Progman
            if (NativeMethods.SetParent(hwnd, _progman) == IntPtr.Zero)
                return false;

            // Z-order: wallpaper below SHELLDLL_DefView
            uint flags = NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE;
            NativeMethods.SetWindowPos(hwnd, _shellDLL_DefView, 0, 0, 0, 0, flags);

            EnsureWorkerWZOrder();
        }
        else
        {
            // Classic: parent to WorkerW
            if (NativeMethods.SetParent(hwnd, _workerW) == IntPtr.Zero)
                return false;
        }
        return true;
    }

    private void EnsureWorkerWZOrder()
    {
        if (!_isRaisedDesktop || _workerW == IntPtr.Zero)
            return;

        // Make sure WorkerW is at the bottom of Progman's children
        IntPtr lastChild = IntPtr.Zero;
        NativeMethods.EnumChildWindows(_progman, (hWnd, lParam) =>
        {
            lastChild = hWnd;
            return true;
        }, IntPtr.Zero);

        if (lastChild != _workerW)
        {
            uint flags = NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE;
            NativeMethods.SetWindowPos(_workerW, NativeMethods.HWND_BOTTOM, 0, 0, 0, 0, flags);
        }
    }

    /// <summary>
    /// Reset the desktop layer (recover after explorer crash, etc.)
    /// </summary>
    public void Reset()
    {
        Initialize();
    }
}
