# Security / 安全

## Supported versions / 支持版本

| Version line | Support |
| --- | --- |
| `v1.1.x` | Current supported line / 当前支持版本线 |
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
