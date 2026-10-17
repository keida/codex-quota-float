# Codex Float R2

**版本：**`2.0.0-beta.1` · **状态：**GitHub 预发布版已发布（非稳定版）；R1 v1.1.3 仍是 Latest。

Codex Float R2 是 Windows 桌面伴侣，用紧凑浮窗显示 Codex 额度和本地观察到的 token 计数活动。它是独立的 Tauri 2 / Rust / React + TypeScript 实现，不替代 WPF R1 产品线。

## 下载

R1 v1.1.3 仍是项目稳定版 Latest Release：[从 GitHub 下载并校验](https://github.com/keida/codex-quota-float/releases/tag/v1.1.3)。R2 `2.0.0-beta.1` 已作为预发布版发布，不是稳定版替代品。[打开 R2 预发布页](https://github.com/keida/codex-quota-float/releases/tag/v2.0.0-beta.1)、[下载 Windows x64 EXE](https://github.com/keida/codex-quota-float/releases/download/v2.0.0-beta.1/Codex-Float-R2-2.0.0-beta.1-windows-x64.exe)，或下载 [SHA256SUMS.txt](https://github.com/keida/codex-quota-float/releases/download/v2.0.0-beta.1/SHA256SUMS.txt)。资产大小为 20,928,512 字节，SHA-256 为 `5BF6AAFB87791556F543863FF7D8691CA363CFB2727A4351DB00A9C2FF36099A`。它未签名、未打包，属于启用 custom-protocol 的 debug-profile 构建；不是已签名安装程序或自包含应用。R2 需要 Windows x64、WebView2、可用的本地 Codex 身份验证和网络连接。证据和限制见 [Acceptance](docs/r2/ACCEPTANCE.md)。

## R2 当前功能

- 显示 96×96 DIP 紧凑浮窗；悬停展开额度详情，移开后恢复紧凑状态。Settings 编辑期间保持打开，通过 Done/Escape 正常关闭。
- 从源码实现的 Codex usage 视图读取动态额度和 reset-credit 数据。刷新状态区分完整、部分、过期、不可用、退出登录和速率限制等结果。
- 将当前返回的套餐标识显示为 `PRO LITE`、`PLUS`、`PRO (MORE)` 或 `PRO (MAX)`，分别对应 `prolite`、`plus`、`pro`、`promax`（不区分大小写）。未知标识仍可读；该标签不会推断额度区间、计费或价格。
- 支持 English / 简体中文，以及 System / Light / Dark 主题。System 使用 Windows 返回的原生窗口主题，并跟随 Windows“应用模式/应用颜色”设置。
- 当前 Settings 提供主题、语言和 60 / 120 / 300 秒刷新间隔；源码未实现 Follow Codex、低额度提醒或 About 页面。
- 提供托盘 Open、Refresh、Exit；窗口配置为不在任务栏单独显示按钮。
- 扫描本地 Codex session JSONL 记录中的累计 token 计数字段，并按本地时间绘制一天内的 12 个区间。缺失区间保持未知；覆盖不完整时，显示总量是下限。Token 活动不等于已消耗额度。

这些是源码支持的能力，不代表完整 Plus/Pro 功能等价或所有账户/服务状态都已覆盖。R2 不使用 Codex app-server，也不提供实时审批/会话控制、预测、提醒或“跟随 Codex”功能。

固定版本的 [Codex auth 源码](https://github.com/openai/codex/blob/ce0323f2f2d3b25e82806cd555237da3db4e1897/codex-rs/protocol/src/auth.rs#L130-L145) 将 `KnownPlan::ProLite` 显示为通用名称 `Pro`。R2 有意把原始 `prolite` 标识显示为 `PRO LITE`，以便与显示为 `PRO (MORE)` 的原始 `pro` 区分。这些是 R2 界面标签，不是上游计费术语或权益声明。当前源码更新、R1/R2 对照、运行要求、隐私边界和已知限制见[《2026-10-17 更新》](docs/r2/UPDATE-2026-10-17.md)。

R2 具有独立生命周期：它不会监控 Codex Desktop，也不会在 Codex 关闭时自动退出。关闭 R2 窗口只会将其隐藏；要退出请使用托盘 Exit。额度读取路径使用本地身份验证数据并直接发送身份验证 HTTP 请求，不依赖正在运行的 Codex 进程。身份验证缺失/无效和请求失败会表示为额度可用性状态。这些是源码层面的行为说明，并不代表每种无 Codex 或网络故障场景都经过原生测试。

## 数据与隐私边界

原生源码从 `CODEX_HOME` 或默认 `.codex` 目录读取本地 Codex 身份验证数据以请求额度；只向 [Security](../SECURITY.md) 中记录的两个 ChatGPT usage endpoint 发送身份验证请求。它还会扫描本地 `sessions/*.jsonl` 的有限内容以查找累计 token 计数字段。由于这些文件包含会话记录，**不能承诺程序从不读取或检查含对话内容的字节**。当前解析器用 token-count 记录生成图表，不会把 session 文件内容发送到产品服务器。

此源码版本没有产品 telemetry 或 analytics endpoint。这是当前版本源码的事实，不是对未来版本的承诺。Token 计数不是额度百分比，R2 也不是 ChatGPT 普通聊天或 API 用量监控。公开文档和发行资产不包含账户数据、token、session 文件或原始响应。更新后的 R2 原生截图仍待补充；根目录 README 中的截图展示的是 WPF R1，不是 R2。

使用预览版前请阅读 [R2 数据边界详情](../SECURITY.md#r2-beta-source-data-boundary)。

## 运行桌面候选版

候选版以单个未打包 EXE 提供，但它**不是自包含程序**：需要 Windows x64、Microsoft Edge WebView2 Runtime、可用的本地 Codex 身份验证文件，以及额度请求所需的网络。终端用户运行时不需要另行安装 .NET、Node.js、Rust 或 Visual Studio Build Tools。多数受支持的 Windows 设备可能已安装 WebView2，但不能假设每台设备都有；参见[微软 WebView2 分发说明](https://learn.microsoft.com/microsoft-edge/webview2/concepts/distribution)。此 Beta 候选版尚未在全新设备上验证可移植性。

## 构建与验证

Windows 前置条件和源码构建命令见 [Development](docs/r2/DEVELOPMENT.md)，已观察的验收结果与剩余缺口见 [Acceptance](docs/r2/ACCEPTANCE.md)。应用使用系统 WebView2 runtime。当前 Tauri bundle 有意保持关闭：构建产物是未打包、未签名的可执行文件，没有安装程序或自动更新器。

## R2 文档

- [Changelog](docs/r2/CHANGELOG.md)
- [Development 与图标生成](docs/r2/DEVELOPMENT.md)
- [Acceptance 与限制](docs/r2/ACCEPTANCE.md)
- [2026-10-17 更新](docs/r2/UPDATE-2026-10-17.md)
- [English README](README.md)

OpenAI、Codex、ChatGPT、Microsoft 和 Windows 名称仅用于说明兼容目标，不代表任何关联或背书。
