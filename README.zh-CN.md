# Quote Float

**语言 / Language:** [English](README.md) · 简体中文（当前）

[![WPF CI 状态](https://github.com/keida/codex-quota-float/actions/workflows/wpf-ci.yml/badge.svg?branch=main)](https://github.com/keida/codex-quota-float/actions/workflows/wpf-ci.yml)

Quote Float 是一个原生 Windows WPF 浮窗，让 Codex Plus/Pro 的额度状态以紧凑的 Full 或 Orb 视图保持可见。

![Quote Float WPF R1 实际 WPF 客户区渲染](docs/assets/quote-float-overview-zh.png)

_图示：实际 WPF 客户区渲染，额度数值为示例；未捕获原生 DWM 边框/圆角。_

Quote Float 的 WPF R1 简化版本已完成冻结并发布。当前状态是 **Boss PASS、v1.1.2 RELEASED / LATEST**：版本是自包含的 Windows x64 单文件应用，无需单独安装 .NET。

## 快速开始

1. 下载 [v1.1.2 Windows x64 单文件 EXE](https://github.com/keida/codex-quota-float/releases/download/v1.1.2/Quote-Float-v1.1.2-win-x64.exe) 和 [SHA256SUMS.txt](https://github.com/keida/codex-quota-float/releases/download/v1.1.2/SHA256SUMS.txt)。
2. 直接运行 `Quote-Float-v1.1.2-win-x64.exe`，无需单独安装 .NET。
3. 可选：在下载的 EXE 与 `SHA256SUMS.txt` 所在目录中，用 PowerShell 校验 EXE：

   ```powershell
   $line = Get-Content .\SHA256SUMS.txt | Where-Object { $_ -match 'Quote-Float-v1.1.2-win-x64\.exe$' }
   $expected = ($line -split '\s+')[0]
   $actual = (Get-FileHash .\Quote-Float-v1.1.2-win-x64.exe -Algorithm SHA256).Hash
   if ($actual -ne $expected) { throw 'Checksum mismatch' }
   "SHA-256 OK: $actual"
   ```

## 当前产品契约

- 原生 WPF Civic Wayfinding 浮窗，支持 Plus 与 Pro。
- Full 与 Orb 是由位置、悬停和临时展开状态推导出的显示模式。
- Settings 是固定 `420 × 296 DIP` 的非模态窗口；用户可选择的设置只有语言和刷新间隔，Done 用于关闭窗口。窗口中的已保存/说明文字不是设置状态，也没有 Settings 内的 Refresh 按钮；唯一的手动 Refresh 在 Full 页脚。Auto Refresh 始终开启，只作为说明文字展示。
- Refreshing 保留已有额度；没有已有快照时显示 Loading。
- R1 不包含 Product Scale，也不包含 Billing/Usage 导航、Auto Refresh 开关、Click-through 或主题/行为选择器。

## 运行与开发

WPF 工程位于 [`wpf/QuotaFloat.Wpf.csproj`](wpf/QuotaFloat.Wpf.csproj)。开发说明见 [`docs/DEVELOPMENT.md`](docs/DEVELOPMENT.md)，安全报告说明见 [`SECURITY.md`](SECURITY.md)。当前源码、哈希、删除项和证据边界见 [`docs/wpf-r1/CANDIDATE-MANIFEST.json`](docs/wpf-r1/CANDIDATE-MANIFEST.json)。

真实额度模式读取本地 Codex 认证并访问 ChatGPT usage/reset-credit 服务；不要发布认证文件、token、原始响应、账户截图或私人诊断。`--direct` 用于独立启动，`--watch` 用于观察 Codex 存在状态；应用不会终止 Codex。

## 仓库结构

- `wpf/`：当前 WPF 产品实现及其 WPF 测试。
- `docs/`：公开技术记录、契约、验收证据摘要和发布记录。
- `native/`：历史 WinForms 实现，保留作历史参考，不是当前 WPF 产品实现。

过时的 `design-preview/` 设计预览已移除，不是当前产品截图或发布资产。

## 验收状态

冻结候选的验收摘要见 [`docs/wpf-r1/ACCEPTANCE-SUMMARY.md`](docs/wpf-r1/ACCEPTANCE-SUMMARY.md)，完整契约见 [`docs/wpf-r1/SIMPLIFICATION-CONTRACT.md`](docs/wpf-r1/SIMPLIFICATION-CONTRACT.md)。发布边界见 [`docs/wpf-r1/PUBLICATION-ALLOWLIST-V3.md`](docs/wpf-r1/PUBLICATION-ALLOWLIST-V3.md)。

已验证基线是 Windows 100% / 96 DPI。以下事项明确保持 `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`：

1. 同一真实 Pro 账户 `Fresh -> SignedOut -> Fresh`。
2. 隔离 Codex `Present -> close -> three absence confirmations -> Quote Float response -> restart/recovery`。
3. 原生 Windows 125% / 150% DPI 运行验收。

## 已发布版本

- [v1.1.2 Latest Release](https://github.com/keida/codex-quota-float/releases/tag/v1.1.2)
- [下载 Windows x64 单文件 EXE](https://github.com/keida/codex-quota-float/releases/download/v1.1.2/Quote-Float-v1.1.2-win-x64.exe)
- [SHA256SUMS.txt](https://github.com/keida/codex-quota-float/releases/download/v1.1.2/SHA256SUMS.txt)
- EXE SHA-256：`2FFF59F1B79E232C7ADBA88C49D57332ECA19EE2BE4FCC9E60195B5898D9E9DF`
- SHA256SUMS.txt SHA-256：`3E278A81B55341D57F8635B68A681F126110688E575A313B8E287FA30E7A968A`
- 产品：自包含的 Windows x64 单文件应用，无需单独安装 .NET。
- 托盘菜单跟随当前中英文语言设置。

V3 源码范围和 v1.1.2 托盘本地化修复已经完成合并。v1.1.0 是之前的已发布版本；v1.1.1 tag 未发布且已被 v1.1.2 取代。历史 v1.0.0 tag/Draft Release 保持不变。
