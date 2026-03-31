using System.Windows;
using WeatherWallpaper.Native;

namespace WeatherWallpaper.Services;

/// <summary>
/// Enumerates display monitors using Win32 API.
/// </summary>
internal static class MonitorService
{
    public static List<Models.MonitorInfo> GetMonitors()
    {
        var monitors = new List<Models.MonitorInfo>();

        NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref NativeMethods.RECT lprcMonitor, IntPtr dwData) =>
        {
            var mi = new NativeMethods.MONITORINFOEX();
            mi.Size = System.Runtime.InteropServices.Marshal.SizeOf(mi);

            if (NativeMethods.GetMonitorInfo(hMonitor, ref mi))
            {
                monitors.Add(new Models.MonitorInfo
                {
                    DeviceName = mi.DeviceName,
                    Bounds = new Rect(mi.Monitor.Left, mi.Monitor.Top,
                        mi.Monitor.Right - mi.Monitor.Left,
                        mi.Monitor.Bottom - mi.Monitor.Top),
                    WorkArea = new Rect(mi.WorkArea.Left, mi.WorkArea.Top,
                        mi.WorkArea.Right - mi.WorkArea.Left,
                        mi.WorkArea.Bottom - mi.WorkArea.Top),
                    IsPrimary = (mi.Flags & 1) != 0,
                    Handle = hMonitor
                });
            }
            return true;
        }, IntPtr.Zero);

        return monitors;
    }

    public static Models.MonitorInfo? GetPrimaryMonitor()
    {
        return GetMonitors().Find(m => m.IsPrimary);
    }

    public static Models.MonitorInfo? GetMonitorByDeviceName(string deviceName)
    {
        return GetMonitors().Find(m => m.DeviceName == deviceName);
    }
}
