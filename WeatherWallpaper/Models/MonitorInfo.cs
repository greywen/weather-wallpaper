using System.Windows;

namespace WeatherWallpaper.Models;

public class MonitorInfo
{
    public string DeviceName { get; set; } = string.Empty;
    public Rect Bounds { get; set; }
    public Rect WorkArea { get; set; }
    public bool IsPrimary { get; set; }
    public IntPtr Handle { get; set; }

    public string DisplayName => IsPrimary ? $"{DeviceName} (主显示器)" : DeviceName;

    public override string ToString() => $"{DeviceName} [{Bounds.Width}x{Bounds.Height}]";

    public override bool Equals(object? obj) =>
        obj is MonitorInfo other && DeviceName == other.DeviceName;

    public override int GetHashCode() => DeviceName.GetHashCode();
}
