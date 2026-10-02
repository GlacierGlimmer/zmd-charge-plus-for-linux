# Linux v0.1.0 验证范围 / Validation scope

验证日期：2026-10-02。环境：WSL2 Ubuntu 24.04 x86_64、.NET 8、普通用户、隔离 Xvfb 会话。

| 检查 | 结果及边界 |
| --- | --- |
| 变量审计 | 该环境列出的 339 项变量均有数据或明确的适用状态，缺失值 0，没有渲染占位 `--`。数量随硬件和权限变化。 |
| CPU / 内存 | 实时 Linux 采集，以及计数器重置、CPU 时间差分、kB/Byte 换算样本通过。 |
| 电池 | energy/charge 单位、多电池汇总、混合充放电分别计量、负循环次数排除及缺失传感器样本通过。WSL 的实时电池来自虚拟设备，不等于物理电池实机认证。 |
| GPU | NVIDIA 实时采集；DRM、多显卡选择、MiB 换算和 N/A 排除样本通过。未覆盖所有物理 AMD/Intel/NVIDIA 型号。 |
| 网络 | 普通用户 ICMP 回环、TCP 连接、UDP 回显以及失败状态通过。 |
| API | 公网 IP、DeepSeek 和自定义 HTTP/JSON 使用确定的响应样本；缺 Key、请求失败和缺字段状态通过。未使用用户真实 API Key 或余额。 |
| GUI | 中英文名称、变量库与挂载点名称、剪贴板文本读写、窗口退出、旧模板清理通过。 |
| 配置 | AES-GCM 往返、篡改拒绝、密钥 0600、自启动启停及 AppImage 原始路径通过。 |
| 四种发行包 | 校验和、元数据、desktop 文件及各自解包内容的实际启动通过。 |
| 包内行为 | HUD、X11 点击穿透、静默自启动、单实例唤回设置通过。 |
| AppImage | 额外验证自解包运行入口；没有验证 FUSE 挂载模式。 |
| DEB / RPM | Ubuntu 24.04 APT 模拟安装解析通过；RPM 为元数据与解包运行验证，未在 Fedora/RHEL 原生安装。 |

## 复现

```bash
dotnet run --project Tests/EndfieldChargePlus.PlatformTests.csproj -c Release
bash scripts/audit-linux-variables.sh
bash scripts/package-linux.sh
bash scripts/verify-linux-packages.sh dist/linux-x64
```

GUI 审计会在本机 `artifacts/linux-audit/` 生成逐变量结果和截图。报告可能包含本机用户名、主机名、地址及进程信息，发布问题反馈前请自行脱敏。

本版本运行范围为 Linux x64、glibc 2.35+、OpenSSL 3、X11/XWayland。Wayland 的全局鼠标和置顶行为受合成器约束。传感器拔出、权限或驱动变化后，需要重新检测并调整模板。

English: These are bounded tests on Ubuntu 24.04 under WSL2/Xvfb, not certification for every distribution, compositor or physical GPU. Local metrics are read live; external API responses and unavailable hardware formats use deterministic fixtures. Reproduce the checks above to obtain a report for your own machine.
