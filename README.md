# Weather Wallpaper

将网页作为 Windows 桌面动态壁纸运行的轻量应用。本项目基于 Lively 进行二次开发，聚焦网页天气桌面场景。当前实现基于 WPF + WinForms + WebView2，通过 WorkerW/Progman 窗口层把网页嵌入桌面图标后方，并支持托盘控制、显示器选择、声音开关和开机启动。

## 演示

- 在线查看：<a href="videos/demo.mp4">demo.mp4</a>

<video src="videos/demo.mp4" controls muted loop playsinline></video>

## 功能特性

- 将任意 `http/https` 网页作为桌面壁纸运行
- 支持多显示器环境下选择目标显示器（单次仅运行一个实例/一个目标屏）
- 支持网页声音开关
- 支持写入 Windows 当前用户开机启动项
- 托盘常驻，提供设置、停止、重启、退出入口
- 自动保存上次配置并在启动时自动恢复
- 支持鼠标事件转发，允许桌面壁纸页面交互（鼠标移动/左右键/滚轮）

## 技术栈

- .NET 9（`net9.0-windows10.0.18362.0`）
- WPF（主程序与设置窗口）
- WinForms（隐藏宿主窗体）
- Microsoft.Web.WebView2
- Win32 API（WorkerW/Progman 贴壁纸与输入转发）
- Newtonsoft.Json（本地设置持久化）

## 运行环境

- Windows 10 1903+ 或 Windows 11
- 建议 x64 环境（项目默认 `Platform=x64`）
- 已安装 WebView2 Runtime

开发构建还需要：

- .NET 9 SDK
- Visual Studio 2022（可选，推荐）

## 快速开始

### 1. 获取源码

```bash
git clone <your-repo-url>
cd weather-wallpaper
```

### 2. 还原与构建

```bash
dotnet restore WeatherWallpaper.sln
dotnet build WeatherWallpaper.sln -c Release -p:Platform=x64
```

### 3. 运行

```bash
dotnet run --project WeatherWallpaper/WeatherWallpaper.csproj -c Debug -p:Platform=x64
```

也可以直接用 Visual Studio 打开 `WeatherWallpaper.sln`，选择 `x64` 后运行。

## 使用说明

1. 启动程序后，应用常驻系统托盘。
2. 双击托盘图标，或右键托盘图标选择“设置”。
3. 输入网页地址（未带协议时会自动补全为 `https://`）。
4. 选择目标显示器。
5. 选择是否启用声音。
6. 选择是否开机启动。
7. 点击“应用壁纸”。

托盘菜单支持：

- 设置
- 停止壁纸
- 重启壁纸
- 退出

## 配置与数据位置

应用数据默认保存在当前用户目录：

- 设置文件：`%LOCALAPPDATA%\WeatherWallpaper\settings.json`
- WebView2 用户数据：`%LOCALAPPDATA%\WeatherWallpaper\WebView2Data`

设置文件字段示例：

```json
{
	"Url": "https://weather.anhejin.cn",
	"SelectedMonitorDeviceName": "\\\\.\\DISPLAY1",
	"AudioEnabled": true,
	"AutoStart": false
}
```

开机启动通过注册表项管理：

- `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`
- 键名：`WeatherWallpaper`

## 项目结构

```text
WeatherWallpaper/
	App.xaml(.cs)                 # 应用入口、单实例控制、托盘与自动启动
	SettingsWindow.xaml(.cs)      # 设置界面与应用逻辑
	Models/
		AppSettings.cs              # 设置模型与本地读写
		MonitorInfo.cs              # 显示器信息模型
	Services/
		WallpaperEngine.cs          # WebView2 壁纸引擎
		MonitorService.cs           # 显示器枚举
		StartupService.cs           # 开机启动管理
	Native/
		DesktopWorker.cs            # WorkerW/Progman 集成
		InputForwarder.cs           # 桌面鼠标消息转发
		NativeMethods.cs            # Win32 P/Invoke 定义
```

## 已知限制

- 当前为单实例模式，重复启动会提示“已经在运行中”。
- 当前仅支持单目标显示器运行（不是每块屏幕独立一份壁纸）。
- 输入转发目前仅覆盖鼠标事件，不包含键盘输入。
- URL 校验较轻量：仅自动补协议，不做更深层可达性校验。

## 常见问题

### 1) 启动后看不到壁纸

- 检查 WebView2 Runtime 是否安装。
- 先尝试托盘菜单“重启壁纸”。
- 确认输入 URL 可以在浏览器正常访问。

### 2) 网页没有声音

- 在设置中勾选“启用声音”后重新应用。
- 检查系统音量及对应输出设备。

### 3) 无法开机启动

- 确认当前用户有写入 `HKCU\...\Run` 的权限。
- 重新勾选“开机自动启动”并点击“应用壁纸”。

## 版本

当前项目版本（`WeatherWallpaper.csproj`）：`0.1.1-Beta`

## 上游项目与致谢

- 本项目基于 Lively 做二次开发：<https://github.com/rocksdanister/lively>
- 桌面嵌入相关思路参考了 Lively 的 WinDesktopCore 方案
- Lively 使用 GPL-3.0 许可证，分发衍生版本时请同时遵循上游许可证要求

## 许可证

仓库当前未单独声明开源许可证。建议尽快补充与上游许可证兼容的 `LICENSE` 文件后再分发。
