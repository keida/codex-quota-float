# Codex Quota Float for Windows

**Language:** English · [简体中文](README.zh-CN.md) · [![Release asset downloads](https://img.shields.io/github/downloads/keida/codex-quota-float/total?style=flat-square&amp;label=Release%20downloads&amp;color=2563eb)](https://github.com/keida/codex-quota-float/releases)

A Windows desktop companion for Codex quota and local token activity. Choose stable WPF R1 or the separate Tauri R2 beta.

**Try R2 Beta:** [Release and Windows x64 download](https://github.com/keida/codex-quota-float/releases/tag/v2.0.0-beta.1) · [Direct EXE](https://github.com/keida/codex-quota-float/releases/download/v2.0.0-beta.1/Codex-Float-R2-2.0.0-beta.1-windows-x64.exe) · [SHA256SUMS.txt](https://github.com/keida/codex-quota-float/releases/download/v2.0.0-beta.1/SHA256SUMS.txt)

**Download R1 Stable:** [Latest v1.1.3 Windows x64 EXE](https://github.com/keida/codex-quota-float/releases/download/v1.1.3/Quote.Float.exe) · [Release notes and SHA-256](https://github.com/keida/codex-quota-float/releases/tag/v1.1.3)

## R2 UI preview

![AI-generated R2 PRO LITE example showing one returned weekly quota window](docs/assets/r2-ui-preview-v1.png)

> **AI-generated product illustration with sample data; not a native screenshot.** It does not show runtime behavior or establish visual acceptance. The PRO LITE example shows one returned weekly window; it does not claim every PRO account returns only a weekly window. R2 follows the durations and values in each response. Actual appearance may vary.

<details>
<summary>PLUS example — illustrative 5-hour and weekly windows</summary>

![AI-generated R2 PLUS example with illustrative 5-hour and weekly quota windows](docs/assets/r2-ui-preview-plus-v1.png)

> AI-generated sample illustration, not a native screenshot or runtime/visual acceptance. These example windows are not guaranteed for every PLUS account; R2 follows returned durations and values.

</details>

## R2 at a glance

R2 is a separate Tauri 2 / Rust / React + TypeScript implementation. It displays returned Codex quota windows and read-only reset-credit information, plus a local Today token-activity chart. The chart is not the quota view.

- Hover the compact 96×96 DIP widget to expand quota details; leave it to collapse. Settings stays open while you edit it.
- Manually refresh quota from the expanded header or tray. “Quota updated” is quota-request feedback; it does not mean local activity coverage is complete. Partial, stale, unavailable, and failed refreshes have distinct feedback.
- The tray provides Open, Refresh, and Exit. The window has no taskbar button. Closing its window hides R2; tray Exit quits. R2 does not launch or monitor Codex Desktop.
- The returned plan identifier is shown with UI labels such as PRO LITE, PLUS, PRO (MORE), or PRO (MAX). These labels do not establish billing terms, price, or plan entitlements.

## Quick start

1. Download the R2 EXE and SHA256SUMS.txt from the prerelease links above and verify the EXE against the published checksum before running it.
2. Run the EXE on Windows x64 with Microsoft Edge WebView2 Runtime, usable local Codex sign-in/authentication, and network access. Codex Desktop itself is not launched or monitored by R2.
3. No .NET, Node.js, Rust, or Visual Studio tools are needed for ordinary use. The beta is unsigned, unpackaged, and a custom-protocol debug-profile build—not a self-contained app, installer, or production-ready release. Follow Windows or organization security policy; do not blindly bypass warnings.

## Settings and data

| Setting or view | Behavior |
| --- | --- |
| Appearance | System follows the Windows app-color setting; Light and Dark are also available. |
| Language | English or Simplified Chinese. |
| Refresh interval | 60, 120, or 300 seconds. Failure backoff and applicable server Retry-After can delay the next poll; a fixed interval is not guaranteed. |
| Today activity | Read-only local token activity in 12 local-time buckets. Missing buckets stay blank/unknown; with partial coverage, the total is a lower bound and uses K/M formatting. It does not measure subscription-quota consumption, ordinary ChatGPT chats, API usage, or billing. |
| Reset credits | Read-only returned count/expiry details when valid fields are available; unknown or unavailable does not mean zero. It is not a reset/action button. |

R2 scans bounded local Codex session records for cumulative token-count fields. Session files can contain conversation-bearing bytes; the current parser selects token data and does not send session contents to a product server. The current source has no product telemetry endpoint; that is a statement about this source version, not a promise about future versions.

## FAQ and limits

- **Does quota refresh also verify the Today chart?** No. Quota-request feedback and local-activity coverage are separate.
- **Does R2 need Codex Desktop open?** R2 reads local authentication and makes its quota requests directly; it does not launch or monitor Codex. This describes the source path, not broad native acceptance for every no-Codex scenario.
- **Why is quota missing or stale?** Check local Codex sign-in/authentication and network access. Signed-out, rate-limited, partial, stale, and unavailable are different states; missing data is not zero.
- **Is the Today chart a quota meter?** No. It summarizes locally observed token counts, not quota consumed.

This is a beta. Broad account/provider coverage, clean-machine portability, multiple-DPI behavior, and a complete interaction matrix remain unverified. Read the [R2 README](https://github.com/keida/codex-quota-float/blob/codex/r2-release/r2-tauri/README.md), [feature boundaries](https://github.com/keida/codex-quota-float/blob/codex/r2-release/r2-tauri/docs/r2/UPDATE-2026-10-17.md), and [acceptance record](https://github.com/keida/codex-quota-float/blob/codex/r2-release/r2-tauri/docs/r2/ACCEPTANCE.md) for detail.

## Stable WPF R1

WPF R1 v1.1.3 remains the project's stable Latest release. It provides the original Full and Orb widget, Codex presence/launch options, and its own settings and privacy boundaries. See the [WPF R1 user guide](docs/wpf-r1/USER-GUIDE.md) for its complete requirements, privacy details, verification steps, product contract, and acceptance limits.

## About the download badge

The badge counts downloads of all assets across R1 and R2 releases, including checksum files. It is not a count of unique users or installations, and it is not an R2-only total. Shields.io caching can delay updates.

OpenAI, Codex, ChatGPT, Microsoft, and Windows names identify compatibility targets; no affiliation or endorsement is implied.
