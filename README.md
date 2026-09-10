# Quote Float

**语言 / Language:** 中文（当前） · [English](README.en.md)

Quote Float 的 WPF R1 简化版本已完成冻结并发布。当前状态是 **Boss PASS、v1.1.0 RELEASED / LATEST**：版本发布到 Windows x64 framework-dependent 包，并要求 `.NET 8 Desktop Runtime`。

## 当前产品契约

- 原生 WPF Civic Wayfinding 浮窗，支持 Plus 与 Pro。
- Full 与 Orb 是由位置、悬停和临时展开状态推导出的显示模式。
- Settings 是固定 `420 × 296 DIP` 的非模态窗口；用户可选择的设置只有语言和刷新间隔，Done 用于关闭窗口。窗口中的已保存/说明文字不是设置状态，也没有 Settings 内的 Refresh 按钮；唯一的手动 Refresh 在 Full 页脚。Auto Refresh 始终开启，只作为说明文字展示。
- Refreshing 保留已有额度；没有已有快照时显示 Loading。
- R1 不包含 Product Scale，也不包含 Billing/Usage 导航、Auto Refresh 开关、Click-through 或主题/行为选择器。

## 运行与开发

WPF 工程位于 [`wpf/QuotaFloat.Wpf.csproj`](wpf/QuotaFloat.Wpf.csproj)。开发说明见 [`docs/DEVELOPMENT.md`](docs/DEVELOPMENT.md)。当前源码、哈希、删除项和证据边界见 [`docs/wpf-r1/CANDIDATE-MANIFEST.json`](docs/wpf-r1/CANDIDATE-MANIFEST.json)。

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

- [v1.1.0 Release](https://github.com/keida/codex-quota-float/releases/tag/v1.1.0)
- [下载 Windows x64 ZIP](https://github.com/keida/codex-quota-float/releases/download/v1.1.0/QuoteFloat-WPF-R1-v1.1.0-win-x64.zip)
- [SHA256SUMS.txt](https://github.com/keida/codex-quota-float/releases/download/v1.1.0/SHA256SUMS.txt)
- EXE SHA-256：`322DC1F8ED39A6769CCC1102A6C5D2CABBF088CF2C93003399D50C942CDCE507`
- WPF DLL SHA-256：`E5F50F66903C29F45AEFF799B6738254A8D6CEC0D59A508103B89EC68D250976`
- ZIP SHA-256：`8B6D4AB34A39F9D466A499150968AEA4DDD344FAC1B33B5CDA1696AE5F40024D`

V3 源码范围已经完成合并，v1.1.0 tag 和 Latest Release 已发布。历史 v1.0.0 tag/Draft Release 保持不变。
