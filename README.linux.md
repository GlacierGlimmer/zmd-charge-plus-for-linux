# Endfield Charge Plus

Linux 使用说明 / Linux quick start

## 运行

选择适合系统的 AppImage、目录压缩包、DEB 或 RPM，以当前 Releases 中的实际文件为准。APT 软件源及完整依赖说明见本仓库 README.md。AppImage 需要执行权限；目录包需要完整解压后运行 EndfieldChargePlus，不要单独移动可执行文件或依赖。

Choose the AppImage, directory archive, DEB or RPM offered for your system. See README.md for APT setup and dependencies. AppImage needs execute permission. Extract directory packages completely and run EndfieldChargePlus without separating it from its dependencies.

## 设置与数据

从托盘打开设置，调整显示位置，在“HUD显示内容”选择或创建方案，在“数据管理”管理变量与数据源，然后保存并应用。20 种界面语言和分章节使用手册可在设置中选择。

配置、备份、日志和应用缓存使用 $XDG_DATA_HOME/EndfieldChargePlus，默认 ~/.local/share/EndfieldChargePlus，不自动写入解压目录。相对 XDG 路径不作为程序旁的存储路径。手动导出保存在用户选择的位置。

Open Settings from the tray, choose display settings and HUD profiles, configure variables/data sources, then Save & Apply. Settings include 20 UI languages and a chapter-based User Manual. User data uses $XDG_DATA_HOME/EndfieldChargePlus, defaulting to ~/.local/share/EndfieldChargePlus, independently of extracted application files. Explicit exports use the chosen destination.

## 兼容性与权限

X11/Wayland、桌面环境、显卡驱动及系统接口会影响热区、桌面层、剪贴板、窗口及帧率功能。硬件不支持或权限不足时显示不可用，不会用虚假数值代替。请按手册处理具体功能提示，不要为了通用监控关闭系统安全机制。

Desktop behavior and collectors depend on X11/Wayland, desktop environment, graphics drivers and available system interfaces. Unsupported or inaccessible readings stay unavailable. Follow the manual for the particular feature rather than disabling system security.

## 升级、隐私与反馈

升级或移动前先从托盘退出。保留原用户数据；需要登录启动时，在新位置重新设置。DeepSeek 密钥使用本地 AES-GCM 保护，跨设备一般需重新输入。自定义 URL、请求头和日志仍可能包含敏感内容，分享前请检查。

Exit from the tray before upgrading or moving files. Retain user data and update login-startup settings after moving the application. Local AES-GCM protects the DeepSeek key; enter it again on another device. Review custom URLs, headers and logs before sharing.

详细隐私说明 / Privacy: PRIVACY.md
来源与许可 / Attribution and licenses: NOTICE.md、LICENSE
源码与下载 / Source and releases: https://github.com/GlacierGlimmer/zmd-charge-plus-for-linux
问题反馈 / Issues: https://github.com/GlacierGlimmer/zmd-charge-plus-for-linux/issues
开发与维护 / Developer and maintainer: GlacierGlimmer冰川雪貓
