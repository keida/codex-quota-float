# Security / 安全

## Supported versions / 支持版本

| Version line | Support |
| --- | --- |
| `v1.1.x` | Current supported line / 当前支持版本线 |
| `2.0.0-beta.1` | Preview only; not a stable support commitment / 仅预览，不构成稳定支持承诺 |
| Historical `v1.0.0` | Not supported / 不提供支持 |

## Reporting a concern / 报告安全问题

Private vulnerability reporting is enabled for this repository. If you find a possible security issue, use the [Report a vulnerability form](https://github.com/keida/codex-quota-float/security/advisories/new). Do not open a public issue for a vulnerability or include tokens, cookies, `auth.json`, account payloads, raw API responses, account screenshots, machine-local diagnostics, credentials, or a sensitive proof of concept in any public report.

本仓库已启用私密漏洞报告。如果发现潜在安全问题，请使用 [Report a vulnerability 表单](https://github.com/keida/codex-quota-float/security/advisories/new)。不要为漏洞创建公开 issue，也不要在任何公开报告中提交 token、cookie、`auth.json`、账户数据、原始响应、账户截图、机器本地诊断信息、凭据或敏感的概念验证。

## Data boundary at v1.1.0 / v1.1.0 数据边界

The current WPF source reads local Codex `auth.json` from `CODEX_HOME` or the user's `.codex` directory, then sends bearer authentication only to these documented ChatGPT endpoints:

- `https://chatgpt.com/backend-api/wham/usage`
- `https://chatgpt.com/backend-api/wham/rate-limit-reset-credits`

User preferences are stored locally at `%LOCALAPPDATA%\QuotaFloat\preferences.json`. The v1.1.0 WPF source does not define a telemetry or analytics endpoint; this is a statement about the current source, not a promise about future versions.

当前 WPF 源码从 `CODEX_HOME` 或用户 `.codex` 目录读取本地 Codex `auth.json`，并且只向上述已记录的 ChatGPT endpoint 发送 bearer 身份验证。用户偏好保存在 `%LOCALAPPDATA%\QuotaFloat\preferences.json`。v1.1.0 WPF 源码没有定义 telemetry 或 analytics endpoint；这只是当前源码事实，不代表对未来版本的承诺。

## R2 beta source data boundary / R2 Beta 源码数据边界

R2 is a separate Tauri/Rust implementation. Its native quota reader reads local Codex authentication data from `CODEX_HOME` or the default `.codex` directory and makes authenticated requests only to the two ChatGPT usage endpoints above. The current source contains no product telemetry or analytics endpoint; this describes `2.0.0-beta.1` source only and is not a future-version promise.

R2 also walks a bounded portion of local `sessions/*.jsonl` files to find `event_msg` token-count records and the cumulative `total_token_usage.total_tokens` value used by the local activity chart. The implementation limits discovery to 64 candidate files, 8 MiB total bytes, 1 MiB per file, and 256 KiB per line, and uses a two-second cooperative scan deadline checked between operations. Synchronous filesystem calls may exceed that elapsed-time target. Since these files contain serialized session records, this is **not** a guarantee that conversation-bearing bytes are never read or inspected. The chart uses extracted token-count fields; session-file content is not sent to a product server. Counts can be partial lower bounds and are not quota usage.

R2 has no Codex app-server integration. It does not provide real-time approval/session control, prediction, reminders, or Codex-follow behavior. Account switching and broad native DPI acceptance are not claimed. Do not publish local authentication data, tokens, raw responses, session files, account screenshots, or private diagnostics.

R2 是独立的 Tauri/Rust 实现。原生额度读取器从 `CODEX_HOME` 或默认 `.codex` 目录读取本地 Codex 身份验证数据，并且只向上方两个 ChatGPT usage endpoint 发送身份验证请求。当前源码没有产品 telemetry 或 analytics endpoint；这仅描述 `2.0.0-beta.1` 源码，不构成对未来版本的承诺。

R2 还会有限度地遍历本地 `sessions/*.jsonl` 文件，查找 `event_msg` token-count 记录及本地活动图使用的累计 `total_token_usage.total_tokens`。实现限制为最多 64 个候选文件、总计 8 MiB、单文件 1 MiB、单行 256 KiB；两秒扫描期限会在操作之间协作式检查，同步文件系统调用可能使实际耗时超过该目标。由于这些文件包含序列化会话记录，**不能保证程序从不读取或检查含对话内容的字节**。图表使用提取出的 token-count 字段；不会把 session 文件内容发送到产品服务器。计数可能是部分下限，并不等于额度使用量。

R2 不集成 Codex app-server，不提供实时审批/会话控制、预测、提醒或跟随 Codex 功能；也不宣称已完成账户切换或广泛原生 DPI 验收。不要公开本地身份验证资料、token、原始响应、session 文件、账户截图或私人诊断信息。
