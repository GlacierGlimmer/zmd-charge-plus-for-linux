# ECP For Linux v0.1.1

正式版 · v0.1.0 → v0.1.1 · 2026-10-13

## 简体中文：本次变化

- 新增可自定义数据采样间隔，多个 HUD 共用采样服务，避免重复采集；调整设置时保留各实例自己的配置。
- 新增「实验性功能 → 多实例 HUD」：每个 HUD 可独立选择显示器、方案、位置、字体、缩放与显示层。设置窗口存在且有多个 HUD 时，正在编辑的实例持续显示边框和「当前正在设置」提示。
- 修复多实例切换、关闭设置、删除实例时的状态与生命周期；各实例的常驻置顶／桌面层独立恢复。修复点击遮罩干扰、刷新闪烁及透明背景变黑。
- 新增并修正常驻点击 HUD 切换方案：只响应 HUD 本体点击，对所有实例有效；按被点击实例的轮播队列及动画切换，点击后重新计时。无需开启自动轮播。
- 修复字体选择不生效；增加工作区与屏幕绝对位置预设，切换锚点清除残留微调，避免 Y 意外保留 80。
- 新增实际显示器／窗口 FPS 变量与方案、有效目标选择及满量程 FPS 设置；清除已失效的目标。无法获取的数据汇总到一个提示框，不以刷新率或虚假零值代替。
- 设置窗口标题显示当前版本；实际提升权限后追加「（Endministrator Mode）」；中英文界面保持一致。
- 新增「时间 - 世界时钟」，39 个中英文固定 UTC 偏移选项，保留原有目标时间模式；内置方案按系统、时间、探测、DeepSeek 等类别排序，保留用户轮播顺序。
- DeepSeek 时段按官方通知与北京时间判断，周末和中国法定节假日为低谷；内置日历至 2030 年并支持后台更新。未公布年份只使用已确定的法定日期，不假定调休安排。
- 修复托盘／菜单栏入口、配置目录与启动项处理，并按本平台实际能力筛选变量；不保留无真实读取实现的占位值。

## English: changes since v0.1.0

- Add configurable sampling intervals shared by all HUDs, with independent instance settings.
- Add Experimental features / Multiple HUDs with per-instance monitor, profile, position, font, scale and layer. The selected HUD stays outlined and labelled while Settings is open.
- Fix instance lifecycle, layer restoration, click masks, refresh flicker and black transparency backgrounds.
- Enable persistent-HUD click cycling for every instance. Only the visible capsule accepts clicks; each instance uses its own rotation order and transition, and resets its timer after a click. Automatic rotation can remain off.
- Fix font application and stale position offsets; add work-area and absolute-screen anchors.
- Add real display/window FPS profiles, current target lists and configurable full-scale FPS. Aggregate unavailable variables into one dialog; never substitute refresh rate or fabricated zeroes.
- Show the version in Settings titles and append “（Endministrator Mode）” only for elevated sessions in both languages.
- Add World clock with 39 bilingual fixed UTC offsets and the existing target-time modes. Group built-ins by category without rewriting saved rotation queues.
- Use DeepSeek’s official Beijing-time schedule, excluding weekends and Chinese statutory holidays. Include calendars through 2030 plus validated background updates; unannounced years do not invent compensating-workday arrangements.

## 运行与兼容性 / Requirements and compatibility

提供 Linux x64（glibc 2.35+）四种包及签名 APT 源；实际 FPS 使用 X11 Present，原生 Wayland 不支持该采集。窗口定位／桌面层依赖窗口管理器。UTC 选项为固定偏移。

Linux x64/glibc 2.35+; four package formats and the signed APT repository. Real FPS uses X11 Present; native Wayland collection is unsupported. Window positioning/layers depend on the window manager. UTC offsets are fixed.

## 验证 / Validation

原生 Linux 采集、变量、GUI 和包完整性检查通过；在 Ubuntu/WSL/Xvfb 验证，不等于所有桌面发行版和 GPU 机型实机认证。DeepSeek 日历 201 项检查通过。

每个平台单独提供 SHA-256 校验文件。Use the platform-specific SHA256SUMS file to verify downloaded assets.

[完整源码变化 / Source comparison](https://github.com/GlacierGlimmer/zmd-charge-plus-for-linux/compare/v0.1.0...v0.1.1)
