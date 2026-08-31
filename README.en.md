# Quota Float

**Keep your Codex quota in sight, without filling your desktop.**

[中文](README.md) · English

A native Windows desktop widget that reads your remaining 5-hour and weekly quota using your local Codex session. Choose a full panel, a compact pill, or an edge-friendly orb.

> Version 0.3.0 is a development prerelease under the [MIT license](LICENSE). No binary Release is available yet. This is an unofficial project, not affiliated with or endorsed by OpenAI.

## Three sizes, both quotas visible

| Mode | Default size (logical pixels) | Best for |
| --- | --- | --- |
| Full panel | 306 × 286 | Both quotas, progress bars, reset times, and reset-credit details |
| Pill | 216 × 76 | Both percentages and progress bars with less screen coverage |
| Orb | 80 × 80 | Staying near an edge while keeping 5-hour and weekly quota distinct |

Drag any full-panel edge or corner to resize proportionally between 100% and 180%, constrained by the screen's work area. Comfortable density, status notices, and Windows DPI affect actual dimensions.

- **Readable remaining quota:** percentages, progress bars, and independent warning colors. The full panel includes countdowns and local reset times. Unknown data is not shown as zero.
- **Easy mode switching:** use the header's `◉` button or the three direct mode buttons in Settings. Hover expansion is optional.
- **Collapse at an edge:** drag the full panel by its top header to a work-area edge and release to switch to an orb. Edge collapse and edge snapping are separate options.
- **Native window controls:** always on top, system tray, click-through with tray recovery, position recovery, and single-instance behavior. Settings is an owned dialog displayed in front of the widget.
- **Appearance options:** dark, light, or system theme; ink, soft, and terminal skins; Chinese and English; font-size and density settings.
- **Measured polling:** a default five-minute refresh interval, faster checks near a reset, failure backoff, and optional low-quota notifications.

## Follow Codex

Enable the follow-at-login option on the window-behavior page in Settings and confirm it to register startup for your current Windows user.

- The widget appears when Codex has a visible desktop window. Minimizing Codex keeps the widget open.
- When its last window disappears, two consecutive absent checks close the widget.
- With automatic following enabled, a background watcher remains after Codex closes so the widget can return next time. It makes no quota requests while Codex is absent.
- Disable login startup in Settings, or choose “Stop following” in the tray to end the current watcher. Neither action closes Codex.

This is not a zero-background-process mode. The app installs no service and does not modify Codex files or existing shortcuts.

## Get started

Get the source from the [GitHub repository](https://github.com/keida/codex-quota-float) and build it using the instructions below. A prebuilt EXE download is not available yet.

You need Windows x64, an installed and signed-in Codex Desktop, and the **.NET 8 Desktop Runtime**. Presence detection currently targets the official `OpenAI.Codex` MSIX installation, not arbitrary CLI sessions.

1. Place the built `QuotaFloat.exe` in a directory you intend to keep.
2. Double-click it to attach to an existing Codex session, or attempt to launch the installed Codex app if it is not running.
3. Open Settings to adjust appearance, modes, and following. Login startup is opt-in, not installed automatically.

Before moving or removing the EXE, disable login startup from the version at its old location. The binary is currently unsigned; verify its source if Windows or security software warns you. Do not disable system protection.

## Privacy and permissions

This is **not an offline tool**: it reads your local session and queries ChatGPT over HTTPS.

- Reads `CODEX_HOME/auth.json`, falling back to `.codex/auth.json` under your user directory. It does not ask you to paste tokens or modify your login.
- Credentials are used in memory for fixed quota-query GET requests to `chatgpt.com`, not sent to a project-author server. TLS validation stays enabled and HTTP redirects are disabled.
- Does not purchase or redeem reset credits, send model requests, or stop your tasks.
- Preferences store appearance, behavior, and position only. There is no telemetry upload. Local lifecycle logs exclude credentials and account details.
- Explicit diagnostic reports include quota and process-resource snapshots. **Do not publicly share raw reports, login files, or real-account screenshots.**

Quota queries rely on endpoints that are not a stable public API. Codex or server changes may break compatibility; the official interface remains authoritative.

## Build and check

On Windows with the .NET 8 SDK, run from the repository root:

```powershell
dotnet run --project native/tests/QuotaFloat.Tests.csproj -c Release
dotnet publish native/QuotaFloat.csproj -c Release -r win-x64 --self-contained false -o release
```

The output is `release/QuotaFloat.exe`: a framework-dependent single-file executable, not an installer bundled with the runtime.

Built with C#, WinForms, and GDI, without an embedded Electron, WebView, or Node backend. No zero-resource claim is made; representative long-running, multi-machine benchmarks are not yet available.

## Limitations and feedback

- No code signing, automatic updates, or signed installer.
- Adjacent-monitor edges, mixed DPI, a complete real Codex close/reopen cycle, and startup after a fresh Windows login need further acceptance testing.
- A report of CMD windows appearing on launch remains unconfirmed; its trigger and origin have not been established or claimed fixed.
- The initial public source contains the native implementation and tests, not the early web prototypes or concept images. It does not promise every prototype feature.

Feedback is welcome through [GitHub Issues](https://github.com/keida/codex-quota-float/issues). Include the app and Windows versions, DPI/monitor layout, reproduction steps, and expected versus actual behavior. Use demo data in screenshots and redact account details. Never attach `auth.json`, tokens, or complete API responses. See the [development guide](docs/DEVELOPMENT.md) for contribution and additional testing instructions.

## Acknowledgments and license

Feature and field exploration was informed by [change-42-yhmm/quota-float](https://github.com/change-42-yhmm/quota-float). This project uses a native C# implementation, does not claim full feature parity, and does not include that project's paid licensing or update services. See [third-party notices](THIRD_PARTY_NOTICES.md) for the referenced revision and license notice.

[MIT](LICENSE) © 2026 keida. Third-party materials retain their respective copyright and license notices.
