# Quote Float

Quote Float 的 WPF R1 候选已完成简化冻结。当前状态是 **Boss PASS、publication HOLD**：完整源代码快照和后续文档更新已记录在本地 rebaseline branch；不执行 push、merge、tag、Release 或外部发布。

## 当前产品契约

- 原生 WPF Civic Wayfinding 浮窗，支持 Plus 与 Pro。
- Full 与 Orb 是由位置、悬停和临时展开状态推导出的显示模式。
- Settings 是固定 `420 × 296 DIP` 的非模态窗口；用户可选择的设置只有语言和刷新间隔，Done 用于关闭窗口。窗口中的已保存/说明文字不是设置状态，也没有 Settings 内的 Refresh 按钮；唯一的手动 Refresh 在 Full 页脚。Auto Refresh 始终开启，只作为说明文字展示。
- Refreshing 保留已有额度；没有已有快照时显示 Loading。
- R1 不包含 Product Scale，也不包含 Billing/Usage 导航、Auto Refresh 开关、Click-through 或主题/行为选择器。

## 运行与开发

WPF 工程位于 [`wpf/QuotaFloat.Wpf.csproj`](wpf/QuotaFloat.Wpf.csproj)。开发说明见 [`docs/DEVELOPMENT.md`](docs/DEVELOPMENT.md)。源码候选、哈希、删除项和证据边界见 [`docs/wpf-r1/CANDIDATE-MANIFEST.json`](docs/wpf-r1/CANDIDATE-MANIFEST.json)。

真实额度模式读取本地 Codex 认证并访问 ChatGPT usage/reset-credit 服务；不要发布认证文件、token、原始响应、账户截图或私人诊断。`--direct` 用于独立启动，`--watch` 用于观察 Codex 存在状态；应用不会终止 Codex。

## 验收状态

冻结候选的验收摘要见 [`docs/wpf-r1/ACCEPTANCE-SUMMARY.md`](docs/wpf-r1/ACCEPTANCE-SUMMARY.md)，完整契约见 [`docs/wpf-r1/SIMPLIFICATION-CONTRACT.md`](docs/wpf-r1/SIMPLIFICATION-CONTRACT.md)。发布边界见 [`docs/wpf-r1/PUBLICATION-ALLOWLIST-V3.md`](docs/wpf-r1/PUBLICATION-ALLOWLIST-V3.md)。

已验证基线是 Windows 100% / 96 DPI。以下事项明确保持 `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`：

1. 同一真实 Pro 账户 `Fresh -> SignedOut -> Fresh`。
2. 隔离 Codex `Present -> close -> three absence confirmations -> Quote Float response -> restart/recovery`。
3. 原生 Windows 125% / 150% DPI 运行验收。

WPF R1 的 Boss PASS 不等于外部发布授权；V3 范围已完成本地 staging 和本地提交，publication HOLD 目前仅表示仍需完成提交树完整性复核、发布审查和单独外部发布授权。
