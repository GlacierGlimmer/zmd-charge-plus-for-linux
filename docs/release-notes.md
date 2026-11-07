# ECP For Linux v0.2.0

**v0.1.1 → v0.2.0 · 构建日期 2026.11.07**

## 主要更新

- 重新设计的深色设置界面：左侧导航、荧光黄强调色和黄/绿/粉三色细节，区分当前系统与软件整体占用。
- 方案管理、自定义模板、变量库、HTTP / JSON 数据源与按章节定位的使用手册。
- 20 种随软件打包的离线语言，默认跟随系统语言；可手动选择并设置全局字体及 UI 缩放。
- 统一的异常与数据提示，保留具体原因、复制详情、日志入口及逐变量“不再提示”。
- 在 HUD 上长按鼠标中键，指向环形栏中的方案，松开切换；长按时间、拖动距离和圆盘大小可配置。
- 自动轮播与手动切换共用队列：点击向下切换；滚轮向上/向下切换上一/下一方案，配合对应方向动画。
- 轮播可选简洁、完整、向上翻滚、向下翻滚四种动画；快捷键可控制显示/隐藏、前后方案、暂停/继续、预览和设置。
- 多实例 HUD 保留各自的方案、队列、位置、字体、显示器与层级；共享的全局设置统一管理。

保留 Linux 原生 /proc、sysfs 等采集方式，不使用 Windows WMI 占位实现。全局键鼠、热区和增强置顶在 X11 / XWayland 下按环境能力工作；Wayland 合成器可能限制全局输入和窗口层级。

## 主要修复

- 修复 X11 扩展按键、鼠标中键/侧键、滚轮切换和事件转发中的问题。
- 加入 X11 错误处理递归保护，保留键鼠事件向原应用转发的语义。
- 改善设置保存失败提示、语言和提示窗口交互、多 HUD 方案状态同步及后台资源释放。

## 安装与升级

[v0.2.0 Releases](https://github.com/GlacierGlimmer/zmd-charge-plus-for-linux/releases/tag/v0.2.0) 提供 `tar.gz`、`AppImage`、`deb`、`rpm`，均包含 .NET 运行时。DEB / RPM 提供应用菜单入口与 `endfield-charge-plus-for-linux` 命令；再次启动会打开现有实例的设置。

AppImage 无 FUSE 时可使用 `--appimage-extract-and-run`。其他系统图形库与安装说明见 [Linux 详细说明](README.linux.md)。

APT 软件源：https://apt.x-neko.com

```bash
curl -fsSL https://apt.x-neko.com/install.sh | sudo bash
sudo apt-get install endfield-charge-plus-for-linux
```

APT 索引与 GitHub Releases 分别更新，以软件源实际版本为准。需要指定 v0.2.0 时可直接安装本次 Releases 的 DEB。

**支持语言（20）：** 简体中文、English、繁體中文、日本語、한국어、Deutsch、Français、Español、Português、Русский、العربية、हिंदी、বাংলা、اردو、Bahasa Indonesia、Tiếng Việt、ไทย、Türkçe、Italiano、فارسی。

## English — changes since v0.1.1

- Redesigned dark settings with a vertical navigation rail, a fluorescent-yellow accent and three-colour details. System usage and the application’s overall usage are shown separately.
- Profile management, template variables, HTTP / JSON sources and a chapter-based offline manual.
- Twenty bundled offline languages, system-language selection and adjustable global font/UI scale.
- Consistent error and data prompts with concrete reasons, copyable details, log access and per-variable dismissal.
- Hold the middle mouse button on the HUD, point to a profile and release to switch. Hold delay, drag distance and ring size are configurable.
- Manual and automatic cycling share a queue. Clicking moves forward; scrolling up/down selects the previous/next profile with the corresponding transition direction.
- Four cycle animations: Simple, Full, Roll Up and Roll Down. Bind show/hide, previous/next, pause/resume, preview and Settings actions.
- Multiple desktop HUDs retain independent profiles, queues, placement, fonts, displays and layers while sharing global settings.
- Fix extended X11 keys, middle/side mouse buttons, scroll switching and event forwarding.
- Guard against recursive X11 error handling while preserving forwarding to the original application.
- Improve save-error reports, language/prompt windows, multi-HUD profile synchronization and idle resource release.

[v0.2.0 Releases](https://github.com/GlacierGlimmer/zmd-charge-plus-for-linux/releases/tag/v0.2.0) provides self-contained x64 tar.gz, AppImage, DEB and RPM packages. DEB/RPM installs include an application-menu entry and the `endfield-charge-plus-for-linux` command. Starting the app again opens the running instance’s settings.

Without FUSE, use the AppImage’s `--appimage-extract-and-run` option. See [the Linux guide](README.linux.md) for system graphics dependencies.

The APT repository is https://apt.x-neko.com. Its index is updated separately from Releases; for an exact v0.2.0 install, use this release’s DEB.

**Languages:** Simplified Chinese, English, Traditional Chinese, Japanese, Korean, German, French, Spanish, Portuguese, Russian, Arabic, Hindi, Bengali, Urdu, Indonesian, Vietnamese, Thai, Turkish, Italian, Persian.

Hardware metrics depend on the platform, drivers and permissions. Check the SHA-256 file before installing. [Website](https://zmd-bar.x-neko.com/) · [Issues](https://ecp-bugs.x-neko.com/).
