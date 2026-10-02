# Endfield Charge Plus For Linux

[简体中文](README.md) · **English**　｜　**Linux x64 · v0.1.0**

An Endfield-inspired Linux desktop HUD based on [zmd-charge](https://github.com/QinAnze/zmd-charge) and [Endfield Charge Plus](https://github.com/GlacierGlimmer/zmd-charge-plus). It supports persistent or on-demand display, animations, custom profiles and automatic rotation.

This repository contains the Linux edition. For Windows, visit [zmd-charge-plus](https://github.com/GlacierGlimmer/zmd-charge-plus).

## Downloads

Get the packages from [Releases](https://github.com/GlacierGlimmer/zmd-charge-plus-for-linux/releases). All four formats include the .NET runtime.

| Format | v0.1.0 | Use |
| --- | --- | --- |
| `.tar.gz` | [Portable archive](https://github.com/GlacierGlimmer/zmd-charge-plus-for-linux/releases/download/v0.1.0/EndfieldChargePlusForLinux-v0.1.0-linux-x64.tar.gz) | Extract and run |
| `.AppImage` | [AppImage](https://github.com/GlacierGlimmer/zmd-charge-plus-for-linux/releases/download/v0.1.0/EndfieldChargePlusForLinux-v0.1.0-linux-x64.AppImage) | Make executable and run |
| `.deb` | [DEB package](https://github.com/GlacierGlimmer/zmd-charge-plus-for-linux/releases/download/v0.1.0/EndfieldChargePlusForLinux-v0.1.0-linux-x64.deb) | Ubuntu / Debian family |
| `.rpm` | [RPM package](https://github.com/GlacierGlimmer/zmd-charge-plus-for-linux/releases/download/v0.1.0/EndfieldChargePlusForLinux-v0.1.0-linux-x64.rpm) | Fedora and compatible RPM systems |

[SHA256SUMS](https://github.com/GlacierGlimmer/zmd-charge-plus-for-linux/releases/download/v0.1.0/SHA256SUMS) · [Detailed Linux guide](README.linux.md) · [Validation scope](docs/linux-validation.md)

## Install

```bash
# Ubuntu / Debian
sudo apt install ./EndfieldChargePlusForLinux-v0.1.0-linux-x64.deb

# Fedora
sudo dnf install ./EndfieldChargePlusForLinux-v0.1.0-linux-x64.rpm

# AppImage
chmod +x EndfieldChargePlusForLinux-v0.1.0-linux-x64.AppImage
./EndfieldChargePlusForLinux-v0.1.0-linux-x64.AppImage

# Portable archive
tar -xzf EndfieldChargePlusForLinux-v0.1.0-linux-x64.tar.gz
cd EndfieldChargePlusForLinux-v0.1.0-linux-x64
./EndfieldChargePlus
```

DEB/RPM installations provide an application-menu entry and the `endfield-charge-plus-for-linux` command. Launching again opens Settings in the existing instance. Settings also has an Exit button.

Without FUSE, run the AppImage with `--appimage-extract-and-run`. Portable builds still need system graphics libraries; see [the dependency list](README.linux.md).

## Features and Linux data

- Animated HUD, simple/full transitions, positioning, scale, opacity, display layers and profile rotation.
- Custom templates, expressions, progress indicators, conditional colors and Chinese/English UI.
- CPU, memory, swap, battery, disk capacity/I/O, network, processes, display, clipboard and device metrics from actual Linux sources.
- DRM/hwmon GPU metrics and NVIDIA `nvidia-smi`, filtered for the selected adapter.
- ICMP/TCP/UDP probes, DeepSeek balance/period information and custom HTTP/JSON sources.
- Single-instance activation, XDG sign-in autostart, configuration import/export/backups and locally encrypted API keys.

The variable library exposes implemented capabilities detected on the current machine. Windows-only, unimplemented and unavailable metrics are removed. Imported profiles have unsupported references cleaned up. Missing sensors never become fabricated zeroes.

The Ubuntu validation environment exposed **339 variables**, all audited for display. This is a machine-specific result, not a fixed count promised on every device. Re-detect capabilities after changing hardware, permissions or installed tools.

Devices without batteries omit the battery preset. GPU presets adapt to available measurements. Missing API credentials, failed requests and failed probes show explicit status instead of invented balances or latency. DeepSeek requires your own valid API key.

## Requirements and validation

Linux **x86_64**, **glibc 2.35+**, OpenSSL 3 and **X11/XWayland**. This targets Ubuntu 22.04/24.04, Debian 12+ and recent Fedora-class desktops. Native Wayland, ARM64, 32-bit and Alpine/musl builds are not provided.

Under XWayland, global pointer access, topmost behavior and top-edge activation depend on the compositor. Tray visibility requires desktop support for AppIndicator/StatusNotifier; relaunch the application to open Settings if the tray is unavailable.

Collectors, GUI behavior and all four package payloads were tested on Ubuntu 24.04 under WSL2/Xvfb. The RPM payload was exercised after extraction; native installation on every distribution or physical GPU has not been verified. See [the validation report](docs/linux-validation.md) for boundaries, including API fixtures and hardware samples.

## Configuration and privacy

Data defaults to `~/.local/share/EndfieldChargePlus`, respecting `XDG_DATA_HOME`. Autostart uses `~/.config/autostart/endfield-charge-plus-for-linux.desktop`, respecting `XDG_CONFIG_HOME`.

Linux API keys use AES-256-GCM with a local mode-0600 key. This is not an OS keyring. Re-enter keys when moving configuration to another machine. See [PRIVACY.md](PRIVACY.md).

## Build

Use Linux x64 with .NET 8 SDK and the tools listed in [README.linux.md](README.linux.md).

```bash
git clone https://github.com/GlacierGlimmer/zmd-charge-plus-for-linux.git
cd zmd-charge-plus-for-linux
dotnet run --project Tests/EndfieldChargePlus.PlatformTests.csproj -c Release
bash scripts/audit-linux-variables.sh
bash scripts/package-linux.sh
bash scripts/verify-linux-packages.sh dist/linux-x64
```

Output: `dist/linux-x64/`, containing all four packages and `SHA256SUMS`. GUI checks require Xvfb and xdotool. The Linux GitHub Actions workflow runs tests and packaging as well.

## Credits and license

Upstream: [QinAnze/zmd-charge](https://github.com/QinAnze/zmd-charge). Windows extension: [GlacierGlimmer/zmd-charge-plus](https://github.com/GlacierGlimmer/zmd-charge-plus).

Licensed under [MIT](LICENSE); see [NOTICE.md](NOTICE.md). This is an unofficial project and is not affiliated with the creators of Arknights: Endfield.
