# Codex Quota Float for Windows

**语言：**简体中文 · [English](README.md) · [![Release asset downloads](https://img.shields.io/github/downloads/keida/codex-quota-float/total?style=flat-square&amp;label=Release%20downloads&amp;color=2563eb)](https://github.com/keida/codex-quota-float/releases)

面向 Codex 额度与本地 token 活动的 Windows 桌面伴侣。可选择稳定版 WPF R1，或独立的 Tauri R2 Beta。

**试用 R2 Beta：**[预发布页及 Windows x64 下载](https://github.com/keida/codex-quota-float/releases/tag/v2.0.0-beta.1) · [直接下载 EXE](https://github.com/keida/codex-quota-float/releases/download/v2.0.0-beta.1/Codex-Float-R2-2.0.0-beta.1-windows-x64.exe) · [SHA256SUMS.txt](https://github.com/keida/codex-quota-float/releases/download/v2.0.0-beta.1/SHA256SUMS.txt)

**下载 R1 稳定版：**[Latest v1.1.3 Windows x64 EXE](https://github.com/keida/codex-quota-float/releases/download/v1.1.3/Quote.Float.exe) · [发布说明与 SHA-256](https://github.com/keida/codex-quota-float/releases/tag/v1.1.3)

## R2 界面预览

![AI 生成的 R2 PRO LITE 示例，展示一个接口返回的周额度窗口](docs/assets/r2-ui-preview-v1.png)

> **AI 生成的产品示意图和样例数据，并非原生截图。** 它不展示实际运行行为，也不构成视觉验收。PRO LITE 示例展示一个接口返回的周窗口；这不表示所有 PRO 账户都只有周窗口。R2 按每次响应返回的时长和值显示，实际外观可能不同。

<details>
<summary>PLUS 示例——示意性的 5 小时与周窗口</summary>

![AI 生成的 R2 PLUS 示例，展示示意性的 5 小时与周额度窗口](docs/assets/r2-ui-preview-plus-v1.png)

> AI 生成的样例示意图，不是原生截图，也不代表运行或视觉验收。这些示例窗口不保证适用于所有 PLUS 账户；R2 按接口实际返回的时长和值显示。

</details>

## R2 简介

R2 是独立的 Tauri 2 / Rust / React + TypeScript 实现。它展示接口返回的 Codex 额度窗口、只读 reset-credit 信息，以及本地 Today token 活动图表。活动图表与额度视图是分开的。

- 浮窗以 96×96 DIP 紧凑状态启动；悬停展开额度详情，离开后折叠。编辑设置时 Settings 会保持打开。
- 可从展开界面的标题栏或托盘手动刷新额度。“额度已更新”是额度请求的反馈，不代表本地活动数据已完整。部分、过期、不可用和失败分别有不同反馈。
- 托盘提供 Open、Refresh、Exit。窗口不在任务栏单独显示按钮。关闭窗口会隐藏 R2；托盘 Exit 才会退出。R2 不启动或监控 Codex Desktop。
- 返回的套餐标识显示为 PRO LITE、PLUS、PRO (MORE) 或 PRO (MAX) 等界面标签；这些标签不确认计费术语、价格或套餐权益。

## 快速开始

1. 从上方预发布链接下载 R2 EXE 与 SHA256SUMS.txt，运行前按发布的校验值核对 EXE。
2. 在 Windows x64 上运行，并确保已安装 Microsoft Edge WebView2 Runtime、本地 Codex 登录/身份验证可用且网络连通。R2 不会启动或监控 Codex Desktop。
3. 日常使用无需 .NET、Node.js、Rust 或 Visual Studio 工具。此 Beta 未签名、未打包，属于启用 custom-protocol 的 debug-profile 构建；它不是自包含应用、安装程序或可称为生产就绪的发行版。请遵循 Windows 或组织安全策略，不要盲目绕过警告。

## 设置与数据

| 设置或视图 | 行为 |
| --- | --- |
| 外观 | System 跟随 Windows 应用颜色设置；也可选择 Light 或 Dark。 |
| 语言 | English 或简体中文。 |
| 刷新间隔 | 60、120 或 300 秒。失败退避及适用的服务器 Retry-After 会延后下一次轮询，不保证固定间隔。 |
| Today 活动 | 只读的本地 token 活动，按本地时间分为 12 个区间。缺失区间保持空白/未知；覆盖不全时总量是下限，格式为 K/M。它不衡量订阅额度消耗、ChatGPT 普通聊天、API 用量或计费。 |
| Reset credits | 有有效返回字段时显示只读计数/到期信息；未知或不可用不代表为零。它不是重置/操作按钮。 |

R2 在有限范围内扫描本地 Codex session 记录，读取累计 token-count 字段。Session 文件可能包含对话内容字节；当前解析器选择 token 数据，不会把 session 内容发送到产品服务器。当前源码没有产品 telemetry endpoint；这是当前源码版本的说明，不是对未来版本的承诺。

## 常见问题与限制

- **刷新额度会同时验证 Today 图表吗？**不会。额度请求反馈与本地活动覆盖情况相互独立。
- **需要打开 Codex Desktop 吗？**R2 读取本地身份验证并直接请求额度，不启动或监控 Codex。此处描述的是源码请求路径，不代表所有未运行 Codex 的场景均已原生验收。
- **为什么额度缺失或过期？**请检查本地 Codex 登录/身份验证和网络连接。退出登录、速率限制、部分、过期和不可用是不同状态；缺失数据不等于零。
- **Today 图表是额度计量器吗？**不是。它汇总本地观察到的 token 计数，不是已消耗额度。

此版本仍为 Beta。广泛账户/服务覆盖、干净机器可移植性、多种 DPI 行为和完整交互矩阵仍未验证。详细信息见 [R2 README](https://github.com/keida/codex-quota-float/blob/codex/r2-release/r2-tauri/README.zh-CN.md)、[功能边界](https://github.com/keida/codex-quota-float/blob/codex/r2-release/r2-tauri/docs/r2/UPDATE-2026-10-17.md) 和 [验收记录](https://github.com/keida/codex-quota-float/blob/codex/r2-release/r2-tauri/docs/r2/ACCEPTANCE.md)。

## 稳定版 WPF R1

WPF R1 v1.1.3 仍是项目稳定版 Latest。它保留原有 Full 与 Orb 浮窗、Codex 状态/启动选项及自身的设置和隐私边界。完整要求、隐私细节、校验步骤、产品契约和验收限制见 [WPF R1 用户指南](docs/wpf-r1/USER-GUIDE.zh-CN.md)。

## 下载徽章说明

此徽章统计 R1 与 R2 所有 Release 资产的下载量，包括校验文件；它不代表独立用户或安装数，也不是 R2 单独下载量。Shields.io 缓存可能导致数字延迟更新。

OpenAI、Codex、ChatGPT、Microsoft 和 Windows 名称仅用于说明兼容目标，不代表任何关联或背书。
