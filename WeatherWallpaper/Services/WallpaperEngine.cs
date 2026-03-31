using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using WeatherWallpaper.Native;
using WinFormsForm = System.Windows.Forms.Form;
using WinFormsFormBorderStyle = System.Windows.Forms.FormBorderStyle;
using WinFormsFormWindowState = System.Windows.Forms.FormWindowState;
using WinFormsDockStyle = System.Windows.Forms.DockStyle;

namespace WeatherWallpaper.Services;

/// <summary>
/// The wallpaper engine manages a hidden WinForms window with WebView2
/// that is embedded into the desktop behind icons.
/// </summary>
public sealed class WallpaperEngine : IDisposable
{
    private readonly DesktopWorker _desktopWorker;
    private WinFormsForm? _hostForm;
    private WebView2? _webView;
    private bool _isRunning;
    private string _currentUrl = string.Empty;
    private Models.MonitorInfo? _currentMonitor;

    public bool IsRunning => _isRunning;
    public string CurrentUrl => _currentUrl;
    public Models.MonitorInfo? CurrentMonitor => _currentMonitor;

    public event EventHandler? WallpaperStarted;
    public event EventHandler? WallpaperStopped;
    public event EventHandler<string>? ErrorOccurred;

    public WallpaperEngine()
    {
        _desktopWorker = new DesktopWorker();
    }

    public async Task StartAsync(string url, Models.MonitorInfo monitor, bool audioEnabled)
    {
        // Stop existing wallpaper first
        Stop();

        try
        {
            // Initialize desktop layer
            if (!_desktopWorker.Initialize())
            {
                ErrorOccurred?.Invoke(this, "无法初始化桌面层 (WorkerW)");
                return;
            }

            _currentUrl = url;
            _currentMonitor = monitor;

            // Create the host form - positioned off-screen initially, no flashing
            _hostForm = new WallpaperHostForm();
            _hostForm.Show();

            // Initialize WebView2
            await InitializeWebView2(audioEnabled);

            if (_webView == null || _hostForm == null)
            {
                ErrorOccurred?.Invoke(this, "WebView2 初始化失败");
                return;
            }

            // Get the form's window handle and attach to desktop
            var handle = _hostForm.Handle;
            if (!_desktopWorker.SetWallpaper(handle, monitor.Bounds))
            {
                ErrorOccurred?.Invoke(this, "无法将壁纸附加到桌面");
                Stop();
                return;
            }

            _isRunning = true;
            WallpaperStarted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"启动壁纸失败: {ex.Message}");
            Stop();
        }
    }

    private async Task InitializeWebView2(bool audioEnabled)
    {
        _webView = new WebView2
        {
            DefaultBackgroundColor = System.Drawing.Color.Black
        };

        // Enable autoplay without user gesture
        var options = new CoreWebView2EnvironmentOptions("--autoplay-policy=no-user-gesture-required");

        // Use a dedicated user data folder
        var userDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WeatherWallpaper", "WebView2Data");

        if (!Directory.Exists(userDataPath))
            Directory.CreateDirectory(userDataPath);

        var env = await CoreWebView2Environment.CreateAsync(null, userDataPath, options);
        await _webView.EnsureCoreWebView2Async(env);

        // Configure settings
        _webView.CoreWebView2.IsMuted = !audioEnabled;
        _webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
        _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        _webView.CoreWebView2.Settings.IsZoomControlEnabled = false;

        // Prevent new windows
        _webView.CoreWebView2.NewWindowRequested += (s, e) => e.Handled = true;
        // Prevent downloads
        _webView.CoreWebView2.DownloadStarting += (s, e) => e.Cancel = true;

        // Navigate
        _webView.CoreWebView2.Navigate(_currentUrl);

        // Add to form
        _hostForm!.Controls.Add(_webView);
        _webView.Dock = WinFormsDockStyle.Fill;
    }

    public void SetMute(bool muted)
    {
        if (_webView?.CoreWebView2 != null)
        {
            _webView.CoreWebView2.IsMuted = muted;
        }
    }

    public void Navigate(string url)
    {
        _currentUrl = url;
        if (_webView?.CoreWebView2 != null)
        {
            _webView.CoreWebView2.Navigate(url);
        }
    }

    public void Stop()
    {
        _isRunning = false;

        if (_webView != null)
        {
            try
            {
                _webView.Dispose();
            }
            catch { }
            _webView = null;
        }

        if (_hostForm != null)
        {
            try
            {
                _hostForm.Close();
                _hostForm.Dispose();
            }
            catch { }
            _hostForm = null;
        }

        WallpaperStopped?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        Stop();
    }
}

/// <summary>
/// A minimal WinForms form that hosts the WebView2 control.
/// Configured to be invisible in taskbar/taskview, starts offscreen to prevent flicker.
/// </summary>
public class WallpaperHostForm : WinFormsForm
{
    public WallpaperHostForm()
    {
        this.Text = "WeatherWallpaper";
        this.FormBorderStyle = WinFormsFormBorderStyle.None;
        this.WindowState = WinFormsFormWindowState.Normal;
        this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
        this.Location = new System.Drawing.Point(-32000, -32000);
        this.Size = new System.Drawing.Size(1, 1);
        this.ShowInTaskbar = false;
        this.ShowIcon = false;
        this.BackColor = System.Drawing.Color.Black;
    }

    protected override System.Windows.Forms.CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW - hide from Alt+Tab
            cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE - don't activate
            return cp;
        }
    }

    protected override bool ShowWithoutActivation => true;
}
