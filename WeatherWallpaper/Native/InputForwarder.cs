using System.Runtime.InteropServices;

namespace WeatherWallpaper.Native;

/// <summary>
/// Forwards mouse events from the desktop to the wallpaper window using a low-level mouse hook.
/// This allows the wallpaper to be interactive while staying behind desktop icons.
/// </summary>
internal sealed class InputForwarder : IDisposable
{
    private IntPtr _hookId = IntPtr.Zero;
    private NativeMethods.LowLevelMouseProc? _hookProc;
    private IntPtr _targetHwnd;
    private IntPtr _progman;
    private IntPtr _workerW;
    private IntPtr _shellDefView;

    public void Start(IntPtr targetHwnd, IntPtr progman, IntPtr workerW, IntPtr shellDefView)
    {
        Stop();

        _targetHwnd = targetHwnd;
        _progman = progman;
        _workerW = workerW;
        _shellDefView = shellDefView;

        // Must keep delegate reference to prevent GC
        _hookProc = HookCallback;
        _hookId = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_MOUSE_LL,
            _hookProc,
            NativeMethods.GetModuleHandle(null),
            0);
    }

    public void Stop()
    {
        if (_hookId != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
        _targetHwnd = IntPtr.Zero;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _targetHwnd != IntPtr.Zero)
        {
            var fgWnd = NativeMethods.GetForegroundWindow();
            if (IsDesktopWindow(fgWnd))
            {
                var hookStruct = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);

                // Only forward if cursor is within the wallpaper window area
                NativeMethods.GetWindowRect(_targetHwnd, out var windowRect);
                if (hookStruct.pt.X >= windowRect.Left && hookStruct.pt.X < windowRect.Right &&
                    hookStruct.pt.Y >= windowRect.Top && hookStruct.pt.Y < windowRect.Bottom)
                {
                    ForwardMouseMessage((uint)wParam.ToInt64(), hookStruct);
                }
            }
        }
        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private bool IsDesktopWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return false;
        if (hwnd == _progman || hwnd == _workerW) return true;
        if (_shellDefView != IntPtr.Zero && hwnd == _shellDefView) return true;

        // Check parent chain (e.g., SysListView32 inside SHELLDLL_DefView)
        var parent = NativeMethods.GetParent(hwnd);
        if (parent != IntPtr.Zero)
        {
            if (parent == _progman || parent == _workerW || parent == _shellDefView) return true;
            var grandparent = NativeMethods.GetParent(parent);
            if (grandparent == _progman || grandparent == _workerW) return true;
        }

        return false;
    }

    private void ForwardMouseMessage(uint msg, NativeMethods.MSLLHOOKSTRUCT hookStruct)
    {
        // Find the deepest child window at cursor position for accurate delivery
        IntPtr target = GetDeepestChild(_targetHwnd, hookStruct.pt);

        var clientPt = hookStruct.pt;
        NativeMethods.ScreenToClient(target, ref clientPt);
        IntPtr lParam = MakeLParam(clientPt.X, clientPt.Y);

        switch (msg)
        {
            case NativeMethods.WM_MOUSEMOVE:
                NativeMethods.PostMessage(target, NativeMethods.WM_MOUSEMOVE, IntPtr.Zero, lParam);
                break;
            case NativeMethods.WM_LBUTTONDOWN:
                NativeMethods.PostMessage(target, NativeMethods.WM_LBUTTONDOWN, (IntPtr)NativeMethods.MK_LBUTTON, lParam);
                break;
            case NativeMethods.WM_LBUTTONUP:
                NativeMethods.PostMessage(target, NativeMethods.WM_LBUTTONUP, IntPtr.Zero, lParam);
                break;
            case NativeMethods.WM_RBUTTONDOWN:
                NativeMethods.PostMessage(target, NativeMethods.WM_RBUTTONDOWN, (IntPtr)NativeMethods.MK_RBUTTON, lParam);
                break;
            case NativeMethods.WM_RBUTTONUP:
                NativeMethods.PostMessage(target, NativeMethods.WM_RBUTTONUP, IntPtr.Zero, lParam);
                break;
            case NativeMethods.WM_MOUSEWHEEL:
                int delta = (short)((hookStruct.mouseData >> 16) & 0xFFFF);
                IntPtr wParamWheel = (IntPtr)(delta << 16);
                // WM_MOUSEWHEEL uses screen coordinates in lParam
                IntPtr screenLParam = MakeLParam(hookStruct.pt.X, hookStruct.pt.Y);
                NativeMethods.PostMessage(target, NativeMethods.WM_MOUSEWHEEL, wParamWheel, screenLParam);
                break;
        }
    }

    /// <summary>
    /// Recursively find the deepest visible child window at the given screen point.
    /// This ensures mouse messages reach the Chrome rendering widget inside WebView2.
    /// </summary>
    private static IntPtr GetDeepestChild(IntPtr parent, NativeMethods.POINT screenPt)
    {
        var clientPt = screenPt;
        NativeMethods.ScreenToClient(parent, ref clientPt);

        var child = NativeMethods.ChildWindowFromPointEx(parent, clientPt, NativeMethods.CWP_SKIPINVISIBLE);
        if (child == IntPtr.Zero || child == parent)
            return parent;

        return GetDeepestChild(child, screenPt);
    }

    private static IntPtr MakeLParam(int x, int y)
    {
        return (IntPtr)((y << 16) | (x & 0xFFFF));
    }

    public void Dispose()
    {
        Stop();
    }
}
