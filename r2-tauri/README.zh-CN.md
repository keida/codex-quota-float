# Codex Float R2

**版本：**`2.0.0-beta.1` · **状态：**源码预览，尚未发布 GitHub Release。

Codex Float R2 是 Windows 桌面伴侣，用紧凑浮窗显示 Codex 额度和本地观察到的 token 计数活动。它是独立的 Tauri 2 / Rust / React + TypeScript 实现，不替代 WPF R1 产品线。

## 下载

R1 v1.1.3 是项目文档记录的已发布 R1 版本：[从 GitHub 下载并校验](https://github.com/keida/codex-quota-float/releases/tag/v1.1.3)。R2 `2.0.0-beta.1` 尚未发布。不要把本地构建或分支候选当作已签名或已发布的安装包。

## R2 当前功能

- 显示 96×96 DIP 紧凑浮窗；悬停展开额度详情，移开后恢复紧凑状态。Settings 编辑期间保持打开，通过 Done/Escape 正常关闭。
- 从源码实现的 Codex usage 视图读取动态额度和 reset-credit 数据。刷新状态区分完整、部分、过期、不可用、退出登录和速率限制等结果。
- 支持 English / 简体中文，以及 System / Light / Dark 主题。System 使用 Windows 返回的原生窗口主题，并跟随 Windows“应用模式/应用颜色”设置。
- 提供托盘 Open、Refresh、Exit；窗口配置为不在任务栏单独显示按钮。
- 扫描本地 Codex session JSONL 记录中的累计 token 计数字段，并绘制本地日活动图。覆盖不完整时，显示总量是下限；token 活动不等于已消耗额度。

这些是源码支持的能力，不代表完整 Plus/Pro 功能等价或所有账户/服务状态都已覆盖。R2 不使用 Codex app-server，也不提供实时审批/会话控制、预测、提醒或“跟随 Codex”功能。

## 数据与隐私边界

原生源码从 `CODEX_HOME` 或默认 `.codex` 目录读取本地 Codex 身份验证数据以请求额度；只向 [Security](../SECURITY.md) 中记录的两个 ChatGPT usage endpoint 发送身份验证请求。它还会扫描本地 `sessions/*.jsonl` 的有限内容以查找累计 token 计数字段。由于这些文件包含会话记录，**不能承诺程序从不读取或检查含对话内容的字节**。当前解析器用 token-count 记录生成图表，不会把 session 文件内容发送到产品服务器。

此源码版本没有产品 telemetry 或 analytics endpoint。这是当前版本源码的事实，不是对未来版本的承诺。Token 计数不是额度百分比，R2 也不是 ChatGPT 普通聊天或 API 用量监控。公开文档和候选资源不包含账户数据、token、session 文件或原始响应。

使用预览版前请阅读 [R2 数据边界详情](../SECURITY.md#r2-beta-source-data-boundary)。

## 运行桌面候选版

候选版以单个未打包 EXE 提供，但它**不是自包含程序**：需要 Windows x64、Microsoft Edge WebView2 Runtime、可用的本地 Codex 身份验证文件，以及额度请求所需的网络。终端用户运行时不需要另行安装 .NET、Node.js、Rust 或 Visual Studio Build Tools。多数受支持的 Windows 设备可能已安装 WebView2，但不能假设每台设备都有；参见[微软 WebView2 分发说明](https://learn.microsoft.com/microsoft-edge/webview2/concepts/distribution)。此 Beta 候选版尚未在全新设备上验证可移植性。

## 构建与验证

Windows 前置条件和源码构建命令见 [Development](docs/r2/DEVELOPMENT.md)，已观察的验收结果与剩余缺口见 [Acceptance](docs/r2/ACCEPTANCE.md)。应用使用系统 WebView2 runtime。当前 Tauri bundle 有意保持关闭：构建产物是未打包、未签名的可执行文件，没有安装程序或自动更新器。

## R2 文档

- [Changelog](docs/r2/CHANGELOG.md)
- [Development 与图标生成](docs/r2/DEVELOPMENT.md)
- [Acceptance 与限制](docs/r2/ACCEPTANCE.md)
- [English README](README.md)

OpenAI、Codex、ChatGPT、Microsoft 和 Windows 名称仅用于说明兼容目标，不代表任何关联或背书。
