using System.Windows;
using WeatherWallpaper.Models;
using WeatherWallpaper.Services;

namespace WeatherWallpaper;

public partial class SettingsWindow : Window
{
    private readonly WallpaperEngine _engine;
    private readonly AppSettings _settings;
    private readonly List<MonitorInfo> _monitors;

    public SettingsWindow(WallpaperEngine engine, AppSettings settings)
    {
        InitializeComponent();

        _engine = engine;
        _settings = settings;
        _monitors = MonitorService.GetMonitors();

        LoadSettings();

        _engine.WallpaperStarted += (s, e) => Dispatcher.Invoke(UpdateStatus);
        _engine.WallpaperStopped += (s, e) => Dispatcher.Invoke(UpdateStatus);
        _engine.ErrorOccurred += (s, err) => Dispatcher.Invoke(() =>
        {
            StatusText.Text = $"错误: {err}";
            StatusText.Foreground = System.Windows.Media.Brushes.Red;
        });

        UpdateStatus();
    }

    private void LoadSettings()
    {
        UrlTextBox.Text = _settings.Url;
        AudioCheckBox.IsChecked = _settings.AudioEnabled;
        AutoStartCheckBox.IsChecked = _settings.AutoStart;

        MonitorComboBox.ItemsSource = _monitors;

        // Select the saved monitor or primary
        var selectedMonitor = !string.IsNullOrEmpty(_settings.SelectedMonitorDeviceName)
            ? _monitors.Find(m => m.DeviceName == _settings.SelectedMonitorDeviceName)
            : null;

        MonitorComboBox.SelectedItem = selectedMonitor ?? _monitors.Find(m => m.IsPrimary) ?? _monitors.FirstOrDefault();
    }

    private void UpdateStatus()
    {
        if (_engine.IsRunning)
        {
            StatusText.Text = $"状态: 运行中 - {_engine.CurrentUrl} ({_engine.CurrentMonitor?.DeviceName})";
            StatusText.Foreground = System.Windows.Media.Brushes.Green;
        }
        else
        {
            StatusText.Text = "状态: 已停止";
            StatusText.Foreground = System.Windows.Media.Brushes.Gray;
        }
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        var url = UrlTextBox.Text.Trim();
        if (string.IsNullOrEmpty(url))
        {
            System.Windows.MessageBox.Show("请输入网页地址", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Ensure URL has protocol
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
            UrlTextBox.Text = url;
        }

        var monitor = MonitorComboBox.SelectedItem as MonitorInfo;
        if (monitor == null)
        {
            System.Windows.MessageBox.Show("请选择显示器", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var audioEnabled = AudioCheckBox.IsChecked == true;
        var autoStart = AutoStartCheckBox.IsChecked == true;

        // Save settings
        _settings.Url = url;
        _settings.SelectedMonitorDeviceName = monitor.DeviceName;
        _settings.AudioEnabled = audioEnabled;
        _settings.AutoStart = autoStart;
        _settings.Save();

        // Update startup
        StartupService.SetStartup(autoStart);

        StatusText.Text = "正在启动壁纸...";
        StatusText.Foreground = System.Windows.Media.Brushes.Orange;

        ApplyButton.IsEnabled = false;
        try
        {
            await _engine.StartAsync(url, monitor, audioEnabled);
        }
        finally
        {
            ApplyButton.IsEnabled = true;
        }
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        _engine.Stop();
    }

    private void HideButton_Click(object sender, RoutedEventArgs e)
    {
        this.Hide();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Hide instead of close - the app runs from tray
        e.Cancel = true;
        this.Hide();
    }
}
