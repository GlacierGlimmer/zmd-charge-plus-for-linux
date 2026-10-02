# Endfield Charge Plus For Linux

Linux x64 版本保留 Avalonia 设置界面、HUD 动画、自定义方案、轮播、网络探测、DeepSeek 和 HTTP/JSON 数据源。四种发行包都包含 .NET 运行时。

## 安装

面向使用 glibc 2.35+、OpenSSL 3 的 Linux 桌面，例如 Ubuntu 22.04/24.04、Debian 12+、较新的 Fedora。需要 X11；Wayland 会话通过 XWayland 运行。暂不提供 ARM64、32 位或 Alpine/musl 包。

| 格式 | 使用方式 |
| --- | --- |
| `.tar.gz` | 解压到固定目录，运行 `./EndfieldChargePlus` |
| `.AppImage` | `chmod +x 文件名.AppImage` 后运行；若 FUSE 不可用，可用 `./文件名.AppImage --appimage-extract-and-run`，或解包后运行 `squashfs-root/AppRun` |
| `.deb` | `sudo apt install ./EndfieldChargePlusForLinux-v0.1.0-linux-x64.deb` |
| `.rpm` | `sudo dnf install ./EndfieldChargePlusForLinux-v0.1.0-linux-x64.rpm` |

DEB/RPM 安装后可从应用菜单或 `endfield-charge-plus-for-linux` 命令启动，安装目录为 `/opt/endfield-charge-plus-for-linux`。卸载使用 `sudo apt remove endfield-charge-plus-for-linux` 或 `sudo dnf remove endfield-charge-plus-for-linux`；个人配置保留。内部程序集与便携包可执行文件仍使用 `EndfieldChargePlus`，以保持资源和配置兼容。

便携包和 AppImage 仍需要系统的 X11、Xext、Xrender、Xrandr、Xi、Xcursor、ICE、SM、fontconfig、xcb、ICU、OpenSSL 3、zlib 和 C/C++ 运行库。Ubuntu 可安装：

```bash
sudo apt install libx11-6 libxext6 libxrender1 libxrandr2 libxi6 libxcursor1 libice6 libsm6 libfontconfig1 libxcb1 libssl3 zlib1g libgcc-s1 libstdc++6 fonts-noto-cjk xdg-utils iputils-ping
# ICU：Ubuntu 22.04 用 libicu70，24.04 用 libicu74。
```

GNOME 若未启用 AppIndicator/StatusNotifier 扩展，托盘可能不可见。再次启动 ECP 可唤回设置，设置页也提供“退出”按钮。XWayland 对全局鼠标位置和置顶行为的限制由桌面合成器决定；鼠标顶部唤出可能受限，可使用持续显示或设置里的预览。

## Linux 数据支持

- CPU 利用率、名称、逻辑/物理核心、频率、调度统计：读取 `/proc/stat`、`/proc/cpuinfo`、cpufreq/hwmon。速率先完成两次真实采样；平均值基于本次运行已采集的样本。
- 内存、缓存、提交量、Linux swap：读取 `/proc/meminfo`；`Committed_AS` 是内核承诺量，并非实际驻留内存。
- 磁盘容量来自实际挂载点，I/O 来自 `/sys/dev/block/*/stat`。动态 Key 为 `disk.mount_<挂载点稳定哈希>.*`；同一设备的多个挂载点不重复累计总容量。
- 电池、交流电状态：读取 `/sys/class/power_supply`，支持多电池及 energy/charge 两种单位。总功率为各电池功率绝对值之和，充/放电功率按各自状态分组汇总；多电池同时充放电时不伪造整机剩余时间。
- GPU：AMD/Intel 等使用 DRM/hwmon，NVIDIA 使用已安装的 `nvidia-smi`。变量库按所选显卡筛选，不把独显显存或利用率硬套给不提供该指标的核显。
- 网络速率、流量、包和错误统计读取 sysfs；地址、连接数使用 Linux 网络接口。时间、网络探测、DeepSeek、自定义 HTTP/JSON 保留。
- 进程 CPU/内存/磁盘排名来自当前用户可读取的 `/proc`；CPU 百分比按逻辑处理器数归一化。USB/输入设备来自 sysfs/procfs，开发者工具数据来自真实进程或已安装命令。
- 屏幕与剪贴板从当前 X11/XWayland 图形会话读取；剪贴板更新时间是本程序观察到变化的时间。即使没有剪贴板管理器也能读写文本。
- 变量库是 Linux 能力白名单：Defender、BitLocker、UAC、Windows 服务/事件日志、WSL 主机统计、DirectX 引擎计数器、Windows 盘符变量以及未实现项均移除。没有传感器、驱动或权限的数据不会作为可选变量保留。硬件/工具变化后使用“重新检测 Linux 变量”或重启。
- 无电池设备不显示电池内置方案；GPU 缺少显存/负载指标时使用真实名称与枚举数量。导入的自定义方案中无效引用会被清理；原设置及导入备份保留。API 数据仍需有效配置，网络失败显示明确状态，不伪造余额、IP 或 `999 ms` 延迟。设备运行中移除或权限变化会让原模板失去数据，需要重新检测并调整方案。
- X11 支持 HUD 点击穿透及全局鼠标顶部唤出；XWayland 是否完整支持取决于合成器。

配置、日志及备份默认位于 `~/.local/share/EndfieldChargePlus`（遵循 `XDG_DATA_HOME`）。开机启动项位于 `~/.config/autostart/endfield-charge-plus-for-linux.desktop`（遵循 `XDG_CONFIG_HOME`）。旧 ECP 启动项会迁移，避免重复启动。AppImage 自启动记录原始 AppImage 路径，移动文件后需重新保存设置。解包运行的 AppImage 按解包后的目录注册。更新检查只认可附带 Linux x64 安装包的 Release。

Linux API Key 使用 AES-256-GCM 加密，本机密钥位于配置目录下 `secrets/master.key`，权限为 `0600`，目录权限为 `0700`。这不等同于系统钥匙串；能读取该用户文件的程序仍可解密。导出配置不含本机密钥，跨设备/跨系统导入后须重新填写 API Key。详见 [隐私说明](PRIVACY.md)。

## 构建与测试

在 Linux x64（或 Windows 的 Ubuntu WSL）安装 .NET 8 SDK，以及 `rpm`、`squashfs-tools`、`desktop-file-utils`、`curl`。打包过程无需 root，第一次会下载官方 appimagetool 1.9.1 并校验 SHA-256；工具会下载 AppImage runtime。

```bash
dotnet run --project Tests/EndfieldChargePlus.PlatformTests.csproj -c Release
bash scripts/audit-linux-variables.sh
bash scripts/package-linux.sh
# 输出：dist/linux-x64/，包含四种包和 SHA256SUMS
# 自定义：VERSION=0.1.0 OUTPUT_DIR=/absolute/output bash scripts/package-linux.sh
```

`PUBLISH_DIR` 可复用已经自包含发布的 `linux-x64` 目录；`APPIMAGETOOL` 可指定本地 appimagetool 1.9.1。发布文件名里的版本必须与复用的程序集版本一致。GitHub Actions 的 Linux 工作流负责测试并生成四种构建产物，不自动发布 Release。

打包和图形冒烟测试说明：`scripts/smoke-linux.sh` 接收程序完整路径，在独立 Xvfb 会话与临时配置目录内验证启动、点击穿透和重复启动唤回设置，需安装 `xvfb`、`xdotool`、`python3`。`bash scripts/verify-linux-packages.sh dist/linux-x64` 校验 SHA-256、四种包内容及 AppImage 启动入口，另需 `cpio`。

## English

Linux x64 packages include the .NET runtime and target glibc 2.35+, OpenSSL 3 and X11/XWayland. Use `apt install ./file.deb`, `dnf install ./file.rpm`, extract the tarball and run `./EndfieldChargePlus`, or mark the AppImage executable and launch it. AppImage extraction is available when FUSE is unavailable.

The Linux library includes only implemented metrics detected on this machine. CPU, memory, swap, battery, mounts, disk I/O, network, process, USB/input, display, clipboard and installed developer tools use real Linux sources. GPU metrics use DRM/hwmon or NVIDIA nvidia-smi and are filtered per adapter. Windows-only and unavailable metrics are removed, including imported template references. Re-detect after hardware/tool changes. API/probe failures show explicit status instead of fabricated numbers. Network probes, templates, animations, rotation, DeepSeek and custom HTTP/JSON remain available. Global pointer access, click-through and topmost behavior under XWayland depend on the compositor. A desktop with StatusNotifier/AppIndicator support is needed for the tray; relaunching ECP reopens Settings, which also has an Exit button.

Settings use `$XDG_DATA_HOME/EndfieldChargePlus` (default `~/.local/share/EndfieldChargePlus`). Autostart uses `$XDG_CONFIG_HOME/autostart` (default `~/.config/autostart`). Linux secrets use AES-256-GCM with a local mode-0600 key, not an OS keyring. Re-enter API keys after importing a configuration onto a different machine/OS.

Build on Linux x64 using .NET 8 SDK: run the platform tests above, then `bash scripts/package-linux.sh`. See [Avalonia Linux documentation](https://docs.avaloniaui.net/docs/platform-specific-guides/linux) and [AppImage packaging documentation](https://docs.appimage.org/packaging-guide/manual.html) for platform background.

The GUI audit emits a per-variable report and screenshots. Local metrics are live; external IP and DeepSeek responses are deterministic HTTP fixtures, and probes use loopback sockets. It also tests missing credentials, request failures and legacy configuration cleanup. Kernel unit references: [power supply class](https://www.kernel.org/doc/html/latest/power/power_supply_class.html), [block statistics](https://www.kernel.org/doc/html/latest/block/stat.html), [NVIDIA SMI](https://docs.nvidia.com/deploy/nvidia-smi/index.html).
