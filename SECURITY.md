# Security / 安全

## Supported versions / 支持版本

| Version line | Support |
| --- | --- |
| `v1.1.x` | Current supported line / 当前支持版本线 |
| Historical `v1.0.0` | Not supported / 不提供支持 |

## Reporting a concern / 报告安全问题

Private vulnerability reporting is not currently enabled for this repository. Do not put tokens, cookies, `auth.json`, account payloads, raw API responses, account screenshots, or machine-local diagnostics in a public issue.

If you find a possible security issue, open a minimal public issue without sensitive details to request a private contact path, or contact the maintainer through the [GitHub profile](https://github.com/keida). Do not include credentials, private account data, or a sensitive proof of concept. We will review the report and provide next steps when a private contact path is available; no fixed response-time SLA is promised.

如果你发现潜在安全问题，请创建不含敏感信息的最小公开 issue，以请求私下沟通渠道，或通过维护者的 [GitHub 个人主页](https://github.com/keida) 联系。不要在公开 issue 中提交 token、cookie、`auth.json`、账户数据、原始响应、账户截图或机器本地诊断信息。仓库目前未启用 GitHub 的私密漏洞报告入口，也不承诺固定响应时限。

## Data boundary at v1.1.0 / v1.1.0 数据边界

The current WPF source reads local Codex `auth.json` from `CODEX_HOME` or the user's `.codex` directory, then sends bearer authentication only to these documented ChatGPT endpoints:

- `https://chatgpt.com/backend-api/wham/usage`
- `https://chatgpt.com/backend-api/wham/rate-limit-reset-credits`

User preferences are stored locally at `%LOCALAPPDATA%\QuotaFloat\preferences.json`. The v1.1.0 WPF source does not define a telemetry or analytics endpoint; this is a statement about the current source, not a promise about future versions.

当前 WPF 源码从 `CODEX_HOME` 或用户 `.codex` 目录读取本地 Codex `auth.json`，并且只向上述已记录的 ChatGPT endpoint 发送 bearer 身份验证。用户偏好保存在 `%LOCALAPPDATA%\QuotaFloat\preferences.json`。v1.1.0 WPF 源码没有定义 telemetry 或 analytics endpoint；这只是当前源码事实，不代表对未来版本的承诺。
