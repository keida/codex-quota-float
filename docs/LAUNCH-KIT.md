# Launch Kit

Drafts only. Nothing in this file has been posted externally.

## Shared facts and links

- Product: **Codex Quota Float for Windows**
- Repository: https://github.com/keida/codex-quota-float
- Latest Release: https://github.com/keida/codex-quota-float/releases/tag/v1.1.2
- Direct EXE: https://github.com/keida/codex-quota-float/releases/download/v1.1.2/Quote-Float-v1.1.2-win-x64.exe
- SHA256SUMS: https://github.com/keida/codex-quota-float/releases/download/v1.1.2/SHA256SUMS.txt

Truth to preserve in every channel:

- Windows x64 only; native Windows WPF; Codex-only; Full/Orb; English/中文.
- v1.1.2 is a self-contained single-file EXE; no separate .NET installation is required.
- The EXE is unsigned and Windows Defender SmartScreen may warn. Tell readers to use the official Release, verify SHA-256, and follow their Windows or organization policy. Never encourage blind bypass.
- Windows 100% / 96 DPI is the verified native baseline. Native 125% / 150% DPI is not verified.
- The current source does not collect chat content or define a telemetry/analytics endpoint. It reads local Codex `auth.json` only for the access token and account-routing identifier needed for two documented quota requests; Quote Float does not write that token to its own preferences.
- Local visual candidates are now available for review at `docs/assets/quote-float-overview-en.png`, `docs/assets/quote-float-overview-zh.png`, `docs/assets/codex-quota-float-demo.gif`, and `docs/assets/codex-quota-float-social-preview.png`. They use illustrative demo values and real current WPF captures at 100% / 96 DPI; they have not been uploaded or posted.

## Reddit — English draft

Suggested title:

> Codex Quota Float for Windows — a native WPF Full/Orb quota widget

Suggested body:

> I built Codex Quota Float for Windows, a small native WPF companion for people who want their Codex Plus/Pro quota visible without keeping a larger window open.
>
> It is Codex-only and Windows x64 only. The current v1.1.2 release is a self-contained single-file EXE with Full and Orb views, edge placement, hover expansion, tray recovery, and English/中文 settings.
>
> Download: https://github.com/keida/codex-quota-float/releases/tag/v1.1.2
>
> The build is unsigned, so SmartScreen may warn on first run. I recommend downloading only from the official Release, verifying the published SHA-256, and then following your local Windows policy. The verified native baseline is Windows 100% / 96 DPI; 125% and 150% DPI are not yet verified.
>
> Privacy is intentionally bounded: the current source reads local Codex auth data only to make the two quota requests documented in the README, does not read chat content, and has no telemetry/analytics endpoint. It does not write the Codex token to Quote Float preferences.
>
> Feedback on first-run clarity, Full/Orb behavior, and Windows display environments is welcome. Please redact tokens, auth files, cookies, account data, raw responses, screenshots with private information, and local diagnostics from reports.

## V2EX — 中文草稿

建议标题：

> 做了一个 Windows 原生 WPF 的 Codex 额度浮窗：Codex Quota Float

建议正文：

> 这是一个面向 Windows x64 的 Codex 专用额度浮窗，项目名是 **Codex Quota Float for Windows**。它使用原生 WPF，提供 Full 完整窗口和 Orb 悬浮球两种视图，支持边缘定位、悬停展开、托盘恢复，以及 English / 简体中文切换。
>
> 当前 v1.1.2 是自包含单文件 EXE，不需要另外安装 .NET：
>
> https://github.com/keida/codex-quota-float/releases/tag/v1.1.2
>
> 当前版本没有代码签名，Windows Defender SmartScreen 可能在首次运行时警告。建议只从官方 Release 下载，先校验 SHA-256，再根据自己的 Windows 或组织策略处理提示，不要盲目绕过安全警告。
>
> 已验证的原生基线是 Windows 100% / 96 DPI；125% 和 150% DPI 尚未完成原生运行验收。隐私边界也写在 README：源码只读取本地 Codex auth 数据来完成两个额度请求，不读取聊天内容，没有 telemetry/analytics endpoint，也不会把 Codex token 写入 Quote Float 偏好设置。
>
> 欢迎反馈安装、Full/Orb 交互、托盘中英文切换和不同 Windows 显示比例下的实际体验。请不要上传 token、auth.json、cookie、账户信息、原始响应或私人诊断。

## Show HN — English draft

Suggested title (must begin with “Show HN”):

> Show HN: Codex Quota Float for Windows — native WPF Full/Orb quota widget

Suggested body:

> Show HN: I made Codex Quota Float for Windows, a Windows x64 native WPF widget for keeping Codex Plus/Pro quota visible in a compact Full or Orb view.
>
> Why: I wanted a small, Codex-focused desktop surface instead of repeatedly opening a larger app just to check quota state.
>
> How it works: the app observes the installed Codex Desktop presence, can launch or attach under its LaunchAndWatch mode, reads the local Codex auth data needed for quota requests, and displays the normalized result. The current source does not read chat content, has no telemetry/analytics endpoint, and stores only its own language and refresh-interval preferences.
>
> Try it: https://github.com/keida/codex-quota-float/releases/tag/v1.1.2
>
> The release is a self-contained single-file EXE, but it is currently unsigned and SmartScreen may warn. Verify the SHA-256 from the official Release before running it. The verified native baseline is Windows 100% / 96 DPI; 125% / 150% DPI and two real lifecycle scenarios remain out of scope for the current acceptance.
>
> I would especially value feedback on whether the installation path, privacy explanation, and Full → edge → Orb → hover flow are clear. No vote request; the goal is honest early feedback on a Windows-only tool.

## X — short draft

> Codex Quota Float for Windows is a native WPF Full/Orb quota widget for Codex Plus/Pro: Windows x64, Codex-only, English/中文, local-first. v1.1.2 is a self-contained single-file EXE. Unsigned/SmartScreen and 125%/150% DPI limits are disclosed; verify SHA-256 first. https://github.com/keida/codex-quota-float/releases/tag/v1.1.2

## Posting checklist

- [ ] Confirm the links still resolve to the current Latest Release.
- [ ] Keep the unsigned-build and DPI limitations in the post or linked README.
- [ ] Do not say “signed”, “VirusTotal checked”, “automatic updates”, “Scoop”, “WinGet”, or “cross-platform”.
- [ ] Do not attach screenshots containing accounts, tokens, raw responses, cookies, or machine-local diagnostics.
- [ ] Do not claim popularity, endorsements, or external validation that has not occurred.
- [ ] Post manually only after choosing a channel and reviewing its rules.
