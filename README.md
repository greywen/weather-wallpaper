**English** | [简体中文](README.zh-CN.md)

# Weather Wallpaper

A lightweight Windows app that runs web pages as animated desktop wallpapers. This project is a focused derivative of Lively, tailored for weather-themed desktop experiences. It uses WPF + WinForms + WebView2, attaches the rendered page behind desktop icons through the WorkerW/Progman window layer, and includes tray controls, monitor selection, audio toggle, and auto-start support.

## Related Project

- [greywen/web-weather](https://github.com/greywen/web-weather)

## Demo

![demo](demo.gif)

## Features

- Run any `http/https` web page as a desktop wallpaper
- Choose the target monitor in multi-monitor environments (one running instance / one target screen at a time)
- Toggle page audio on or off
- Register the app in the current user's Windows startup items
- Keep the app in the system tray with entries for settings, stop, restart, and exit
- Save the last configuration automatically and restore it on startup
- Forward mouse input so the wallpaper page remains interactive (move / left click / right click / wheel)

## Tech Stack

- .NET 9 (`net9.0-windows10.0.18362.0`)
- WPF (main app and settings window)
- WinForms (hidden host window)
- Microsoft.Web.WebView2
- Win32 API (WorkerW/Progman wallpaper attachment and input forwarding)
- Newtonsoft.Json (local settings persistence)

## Requirements

- Windows 10 1903+ or Windows 11
- x64 is recommended (the project defaults to `Platform=x64`)
- WebView2 Runtime installed

For development builds you also need:

- .NET 9 SDK
- Visual Studio 2022 (optional, recommended)

## Quick Start

### 1. Clone the repository

```bash
git clone https://github.com/greywen/weather-wallpaper
cd weather-wallpaper
```

### 2. Restore and build

```bash
dotnet restore WeatherWallpaper.sln
dotnet build WeatherWallpaper.sln -c Release -p:Platform=x64
```

### 3. Run

```bash
dotnet run --project WeatherWallpaper/WeatherWallpaper.csproj -c Debug -p:Platform=x64
```

You can also open `WeatherWallpaper.sln` in Visual Studio, select `x64`, and run it there.

## Usage

1. Launch the app. It stays in the system tray.
2. Double-click the tray icon, or right-click it and choose Settings.
3. Enter a web page URL. If no scheme is provided, the app automatically prefixes it with `https://`.
4. Select the target monitor.
5. Choose whether audio should be enabled.
6. Choose whether the app should start with Windows.
7. Click Apply Wallpaper.

Tray menu entries:

- Settings
- Stop Wallpaper
- Restart Wallpaper
- Exit

## Configuration and Data Paths

Application data is stored under the current user profile by default:

- Settings file: `%LOCALAPPDATA%\WeatherWallpaper\settings.json`
- WebView2 user data: `%LOCALAPPDATA%\WeatherWallpaper\WebView2Data`

Example settings payload:

```json
{
	"Url": "https://weather.anhejin.cn",
	"SelectedMonitorDeviceName": "\\\\.\\DISPLAY1",
	"AudioEnabled": true,
	"AutoStart": false
}
```

Windows startup is managed through the registry:

- `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`
- Value name: `WeatherWallpaper`

## Project Structure

```text
WeatherWallpaper/
	App.xaml(.cs)                 # App entry point, single-instance control, tray, and startup restore
	SettingsWindow.xaml(.cs)      # Settings UI and apply logic
	Models/
		AppSettings.cs              # Settings model and local persistence
		MonitorInfo.cs              # Monitor information model
	Services/
		WallpaperEngine.cs          # WebView2 wallpaper engine
		MonitorService.cs           # Monitor enumeration
		StartupService.cs           # Windows startup management
	Native/
		DesktopWorker.cs            # WorkerW/Progman integration
		InputForwarder.cs           # Desktop mouse message forwarding
		NativeMethods.cs            # Win32 P/Invoke definitions
```

## Known Limitations

- The app is single-instance. Starting it again shows an "already running" message.
- Only one target monitor is supported at a time, not one wallpaper per screen.
- Input forwarding currently covers mouse input only, not keyboard input.
- URL validation is intentionally lightweight: it auto-adds the scheme, but does not perform deeper reachability checks.

## FAQ

### 1. The wallpaper does not appear after startup

- Make sure WebView2 Runtime is installed.
- Try Restart Wallpaper from the tray menu.
- Confirm that the target URL opens normally in a browser.

### 2. The web page has no audio

- Enable audio in settings and re-apply the wallpaper.
- Check system volume and the active output device.

### 3. Auto-start does not work

- Make sure the current user can write to `HKCU\...\Run`.
- Re-enable Start with Windows and click Apply Wallpaper again.

## Versioning

The repository currently declares `0.1.2-Beta` in `WeatherWallpaper.csproj`. Published releases are tag-driven (`v*`), so release packages may be newer than the project file metadata in the repository.

## Upstream and Credits

- This project is derived from Lively: <https://github.com/rocksdanister/lively>
- The desktop embedding approach is based on ideas from Lively's WinDesktopCore implementation
- Lively is licensed under GPL-3.0, so redistributed derivative builds should also follow the upstream license requirements

## License

This repository does not currently include a standalone open-source license file. Adding a `LICENSE` file that is compatible with the upstream license should be treated as a prerequisite before redistribution.
