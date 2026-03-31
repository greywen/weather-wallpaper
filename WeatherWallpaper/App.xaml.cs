using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;
using WeatherWallpaper.Models;
using WeatherWallpaper.Services;

namespace WeatherWallpaper;

public partial class App : System.Windows.Application
{
    private static Mutex? _mutex;
    private System.Windows.Forms.NotifyIcon? _notifyIcon;
    private WallpaperEngine? _engine;
    private AppSettings? _settings;
    private SettingsWindow? _settingsWindow;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Single instance check
        _mutex = new Mutex(true, "WeatherWallpaper_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            System.Windows.MessageBox.Show("Weather Wallpaper 已经在运行中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        _settings = AppSettings.Load();
        _engine = new WallpaperEngine();

        // Setup system tray
        SetupNotifyIcon();

        // Auto-start wallpaper with saved settings
        await AutoStartWallpaper();
    }

    private void SetupNotifyIcon()
    {
        _notifyIcon = new System.Windows.Forms.NotifyIcon();
        _notifyIcon.Text = "Weather Wallpaper";
        _notifyIcon.Visible = true;

        // Use embedded icon or default
        try
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (File.Exists(iconPath))
                _notifyIcon.Icon = new Icon(iconPath);
            else
                _notifyIcon.Icon = SystemIcons.Application;
        }
        catch
        {
            _notifyIcon.Icon = SystemIcons.Application;
        }

        // Context menu
        var contextMenu = new System.Windows.Forms.ContextMenuStrip();

        var settingsItem = new System.Windows.Forms.ToolStripMenuItem("设置");
        settingsItem.Click += (s, e) => ShowSettingsWindow();
        contextMenu.Items.Add(settingsItem);

        contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        var stopItem = new System.Windows.Forms.ToolStripMenuItem("停止壁纸");
        stopItem.Click += (s, e) => _engine?.Stop();
        contextMenu.Items.Add(stopItem);

        var restartItem = new System.Windows.Forms.ToolStripMenuItem("重启壁纸");
        restartItem.Click += async (s, e) => await AutoStartWallpaper();
        contextMenu.Items.Add(restartItem);

        contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        var exitItem = new System.Windows.Forms.ToolStripMenuItem("退出");
        exitItem.Click += (s, e) => ExitApp();
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) => ShowSettingsWindow();
    }

    private void ShowSettingsWindow()
    {
        if (_settingsWindow == null || !_settingsWindow.IsLoaded)
        {
            _settingsWindow = new SettingsWindow(_engine!, _settings!);
        }

        _settingsWindow.Show();
        _settingsWindow.Activate();
        _settingsWindow.WindowState = WindowState.Normal;
    }

    private async Task AutoStartWallpaper()
    {
        if (_engine == null || _settings == null)
            return;

        var monitor = !string.IsNullOrEmpty(_settings.SelectedMonitorDeviceName)
            ? MonitorService.GetMonitorByDeviceName(_settings.SelectedMonitorDeviceName)
            : null;

        monitor ??= MonitorService.GetPrimaryMonitor();

        if (monitor != null && !string.IsNullOrEmpty(_settings.Url))
        {
            await _engine.StartAsync(_settings.Url, monitor, _settings.AudioEnabled);
        }
    }

    private void ExitApp()
    {
        _engine?.Dispose();

        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }

        _mutex?.ReleaseMutex();
        _mutex?.Dispose();

        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _engine?.Dispose();
        _notifyIcon?.Dispose();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}

