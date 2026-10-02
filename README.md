# Endfield Charge Plus For Linux

**简体中文** · [English](README.en.md)　｜　**Linux x64 · v0.1.0**

基于 [zmd-charge](https://github.com/QinAnze/zmd-charge) 和 [Endfield Charge Plus](https://github.com/GlacierGlimmer/zmd-charge-plus) 开发的终末地风格 Linux 桌面悬浮 HUD。支持常驻显示、鼠标顶部唤出、动画切换、自定义方案和多数据轮播。

这是 Linux 独立版本。Windows 版本请前往 [zmd-charge-plus](https://github.com/GlacierGlimmer/zmd-charge-plus)。

## 下载

### APT 软件源（推荐：Debian / Ubuntu / Kali 等）

已提供官方 APT 软件源：**https://apt.x-neko.com**

首次安装只需要两条命令：

```bash
curl -fsSL https://apt.x-neko.com/install.sh | sudo bash
sudo apt-get install endfield-charge-plus-for-linux
```

添加软件源后，后续可直接通过系统包管理器更新：

```bash
sudo apt update
sudo apt upgrade
```

APT 软件源使用独立 GPG 密钥签名，目前提供 **amd64 / x86_64** 软件包。卸载可使用：

```bash
sudo apt-get remove endfield-charge-plus-for-linux
```

### 直接下载

也可以前往 [Releases](https://github.com/GlacierGlimmer/zmd-charge-plus-for-linux/releases) 手动下载。四种格式都包含 .NET 运行时，无需另装 .NET。

| 格式 | v0.1.0 下载 | 适用方式 |
| --- | --- | --- |
| `.tar.gz` | [便携压缩包](https://github.com/GlacierGlimmer/zmd-charge-plus-for-linux/releases/download/v0.1.0/EndfieldChargePlusForLinux-v0.1.0-linux-x64.tar.gz) | 解压后运行 |
| `.AppImage` | [AppImage](https://github.com/GlacierGlimmer/zmd-charge-plus-for-linux/releases/download/v0.1.0/EndfieldChargePlusForLinux-v0.1.0-linux-x64.AppImage) | 添加执行权限后运行 |
| `.deb` | [DEB 安装包](https://github.com/GlacierGlimmer/zmd-charge-plus-for-linux/releases/download/v0.1.0/EndfieldChargePlusForLinux-v0.1.0-linux-x64.deb) | Ubuntu / Debian 系 |
| `.rpm` | [RPM 安装包](https://github.com/GlacierGlimmer/zmd-charge-plus-for-linux/releases/download/v0.1.0/EndfieldChargePlusForLinux-v0.1.0-linux-x64.rpm) | Fedora 等 RPM 系 |

[SHA-256 校验文件](https://github.com/GlacierGlimmer/zmd-charge-plus-for-linux/releases/download/v0.1.0/SHA256SUMS) · [Linux 详细说明](README.linux.md) · [v0.1.0 验证范围](docs/linux-validation.md)

## 安装与启动

Debian / Ubuntu / Kali 等发行版推荐优先使用上面的 APT 软件源；如果使用 Releases 中的安装包，则在下载目录执行对应命令：

```bash
# Ubuntu / Debian / Kali（手动安装 .deb）
sudo apt install ./EndfieldChargePlusForLinux-v0.1.0-linux-x64.deb

# Fedora
sudo dnf install ./EndfieldChargePlusForLinux-v0.1.0-linux-x64.rpm

# AppImage
chmod +x EndfieldChargePlusForLinux-v0.1.0-linux-x64.AppImage
./EndfieldChargePlusForLinux-v0.1.0-linux-x64.AppImage

# tar.gz
tar -xzf EndfieldChargePlusForLinux-v0.1.0-linux-x64.tar.gz
cd EndfieldChargePlusForLinux-v0.1.0-linux-x64
./EndfieldChargePlus
```

DEB/RPM 安装后可从应用菜单或 `endfield-charge-plus-for-linux` 命令启动。再次启动会唤回已有进程的设置窗口，设置页提供退出按钮。

AppImage 环境没有 FUSE 时，可使用 `./文件名.AppImage --appimage-extract-and-run`。便携包仍需要系统图形库，完整依赖见 [Linux 说明](README.linux.md#安装)。

## 功能

- 终末地风格 HUD 动画、简洁/完整动画模式、自定义位置、缩放、透明度及显示层级。
- 常驻显示或按需唤出；支持方案切换、自动轮播和电源状态提示。
- 自定义模板、表达式、进度条和条件变色；中英文界面。
- Linux CPU、内存、swap、电池、磁盘容量与 I/O、网络、进程、显示器、剪贴板及设备数据。
- GPU 使用 DRM/hwmon 或 NVIDIA `nvidia-smi`；按所选显卡的实际能力显示变量。
- ICMP/TCP/UDP 网络探测、DeepSeek 余额与时段、自定义 HTTP/JSON 数据源。
- 单实例、XDG 登录自启动、配置导入/导出/备份、Linux 本地 API Key 加密。

## Linux 变量

变量库只列出已实现、且在当前设备与权限下检测到采集能力的项目。Windows 专属和未实现的变量已移除，导入旧方案时会清理无效引用；不会用假零值填充不存在的传感器。

| 数据 | Linux 来源 |
| --- | --- |
| CPU / 内存 / swap / 进程 | `/proc`、cpufreq、hwmon |
| 电池 / 交流电 / USB / 输入设备 | `/sys`、`/proc/bus/input/devices` |
| 磁盘容量 / I/O | 实际挂载点、`/sys/dev/block/*/stat` |
| GPU | DRM/hwmon、`nvidia-smi` |
| 网络 | 系统接口、sysfs 计数器、实际网络探测 |
| 显示器 / 剪贴板 | 当前 X11/XWayland 图形会话 |
| 开发者工具 | 实际进程、已安装命令的输出 |

不同硬件的可用数量不同。Ubuntu 验证环境中有 **339 项可用变量**通过逐项显示审计，这不是所有设备都保证具备的固定数量。更换硬件、安装工具或调整权限后，可在变量库点击“重新检测 Linux 变量”。

没有电池时不提供电池内置方案；GPU 缺少负载或显存指标时调整为可读取的信息。API 未配置、请求失败、探测超时会显示明确状态，不伪造余额或延迟。DeepSeek 需要自行配置有效 API Key。

## 运行要求

- Linux **x86_64**，glibc **2.35+**、OpenSSL 3；面向 Ubuntu 22.04/24.04、Debian 12+、较新的 Fedora 等桌面环境。
- 需要 **X11**；Wayland 会话通过 **XWayland** 运行。全局鼠标、置顶和顶部唤出行为受桌面合成器限制。
- GNOME 等桌面的托盘显示取决于 AppIndicator/StatusNotifier 支持；即使托盘不可见，也可再次启动程序打开设置。
- 当前未提供 ARM64、32 位、Alpine/musl 或纯原生 Wayland 版本。

已在 Ubuntu 24.04（WSL2 / Xvfb）完成采集、GUI 和四种包的启动验证。RPM 已验证解包内容，尚未在所有发行版、物理硬件或桌面合成器中完成实机测试。详细测试边界见 [验证说明](docs/linux-validation.md)。

## 配置与隐私

配置和日志默认位于 `~/.local/share/EndfieldChargePlus`，遵循 `XDG_DATA_HOME`。登录启动项位于 `~/.config/autostart/endfield-charge-plus-for-linux.desktop`，遵循 `XDG_CONFIG_HOME`。

Linux API Key 使用 AES-256-GCM 与本地 `0600` 权限密钥保存。跨设备导入配置后需重新填写 API Key；本地密钥不等同于系统钥匙串。详见 [隐私说明](PRIVACY.md)。

## 从源码构建

需要 Linux x64、.NET 8 SDK 及打包工具，完整依赖见 [README.linux.md](README.linux.md#构建与测试)。

```bash
git clone https://github.com/GlacierGlimmer/zmd-charge-plus-for-linux.git
cd zmd-charge-plus-for-linux
dotnet run --project Tests/EndfieldChargePlus.PlatformTests.csproj -c Release
bash scripts/audit-linux-variables.sh
bash scripts/package-linux.sh
bash scripts/verify-linux-packages.sh dist/linux-x64
```

输出目录为 `dist/linux-x64/`，包含四种发行包和 `SHA256SUMS`。图形测试需要 Xvfb、xdotool 等工具；仓库中的 Linux GitHub Actions 工作流也会执行测试与打包。

## 致谢与协议

原项目：[QinAnze/zmd-charge](https://github.com/QinAnze/zmd-charge)。Windows 扩展版：[GlacierGlimmer/zmd-charge-plus](https://github.com/GlacierGlimmer/zmd-charge-plus)。

本项目采用 [MIT License](LICENSE)，来源说明见 [NOTICE.md](NOTICE.md)。本项目是非官方衍生软件，与《明日方舟：终末地》官方无隶属关系。
