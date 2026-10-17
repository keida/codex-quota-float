# Codex Quota Float — WPF R1 User Guide

**Language:** English · [简体中文](USER-GUIDE.zh-CN.md) · [Project homepage](../../README.md)

This guide covers the stable WPF R1 line, currently v1.1.3. R2 is a separate Tauri beta; see the [project homepage](../../README.md) for its quick start and status.

## WPF R1 visuals

![WPF R1 Full, Orb, and Settings views](../assets/quote-float-overview-en.png)

![WPF R1 Full to Orb to Settings demo](../assets/codex-quota-float-demo.gif)

_Illustrative sample values; these are WPF R1 captures at 100% / 96 DPI. Native DWM edge appearance is OS-composed and may differ slightly from captured boundaries._

## Latest stable download

**[Latest Release](https://github.com/keida/codex-quota-float/releases/latest)** · v1.1.3 · [Download the self-contained Windows x64 EXE](https://github.com/keida/codex-quota-float/releases/download/v1.1.3/Quote.Float.exe) · [SHA-256 in release notes](https://github.com/keida/codex-quota-float/releases/tag/v1.1.3)

Run `Quote.Float.exe` directly. No separate .NET installation is required. Verify the SHA-256 from the v1.1.3 release notes before running it. This build is unsigned, so Windows Defender SmartScreen may warn on first run; use the official release link, verify the hash, and follow your Windows or organization policy. Do not bypass a security warning blindly.

## Core features

- **Codex companion:** works with the installed Codex Desktop app, can launch it when needed or observe its presence, and never closes it.
- **Full and Orb:** keep Plus or Pro quota visible, with edge placement, hover expansion, tray recovery, and topmost behavior from the WPF R1 contract.
- **English / 中文:** switch the UI and tray-menu language from Settings; the selection is stored locally.
- **Local-first quota requests:** fetches data only for the supported Codex usage and reset-credit views. The R1 source does not collect chat content or define a product analytics endpoint.

## Windows requirements

- Windows x64. Codex Desktop must be installed when using LaunchAndWatch.
- Native baseline verified at Windows 100% / 96 DPI.
- Windows 125% / 150% native DPI runtime acceptance remains **NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE**.
- The published v1.1.3 EXE is self-contained; no separate .NET installation is required.

## R1 privacy boundary

WPF R1 reads local Codex `auth.json` under `CODEX_HOME` or the user's `.codex` directory to obtain the access token and account-routing identifier needed for quota requests. It sends authenticated GET requests only to:

- `https://chatgpt.com/backend-api/wham/usage`
- `https://chatgpt.com/backend-api/wham/rate-limit-reset-credits`

The current R1 source does not read chat content, define a telemetry or analytics endpoint, or write the Codex token to Quote Float preferences. It stores language and refresh-interval preferences locally at `%LOCALAPPDATA%\QuotaFloat\preferences.json`. Optional Edge Drag diagnostics are off by default; only `QF_EDGE_DRAG_LOG` writes to the user-selected local file. That file may contain timestamps, window/display bounds, and window handles; it is never auto-uploaded. Inspect and redact it before public sharing. Never attach `auth.json`, tokens, cookies, raw responses, account screenshots, or private diagnostics to a public issue.

## Verify the download

From the folder containing the downloaded `Quote.Float.exe`, compare its SHA-256 with the value in the [v1.1.3 release notes](https://github.com/keida/codex-quota-float/releases/tag/v1.1.3):

```powershell
$expected = 'D3F3F2D30662B1C93D973BA68AADA0498E7860245A8325546BE596A3E605E385'
$actual = (Get-FileHash .\Quote.Float.exe -Algorithm SHA256).Hash
if ($actual -ne $expected) { throw 'Checksum mismatch' }
"SHA-256 OK: $actual"
```

## Quick start

1. Download `Quote.Float.exe` from the official [v1.1.3 release](https://github.com/keida/codex-quota-float/releases/tag/v1.1.3). Its SHA-256 is in the release notes.
2. Verify the EXE using the PowerShell snippet above.
3. Run the EXE. Use `--direct` to start the widget independently, or `--watch` to observe Codex without launching it. With no mode flag, the approved LaunchAndWatch behavior is used.

## Product contract

- Settings is a fixed `420 × 296 DIP` non-modal window. The user-selectable settings are language and refresh interval; Done closes the window.
- Saved/explanatory notes are not a settings status. Settings has no Refresh button; the sole manual Refresh action is in the Full footer. Auto Refresh is always on.
- Refreshing retains accepted quota; without a prior snapshot, the active request shows Loading.
- R1 excludes Product Scale, Billing/Usage navigation, the Auto Refresh toggle, Click-through, and theme/behavior selectors.

## Technical records and verification

- [Public roadmap](../../ROADMAP.md)
- [Acceptance summary](ACCEPTANCE-SUMMARY.md)
- [R1 release notes](RELEASE-NOTES-R1.md)
- [Development guidance](../DEVELOPMENT.md)
- [Current candidate manifest](CANDIDATE-MANIFEST.json)
- [Security reporting](../../SECURITY.md)
- [License](../../LICENSE)
- [Third-party notices](../../THIRD_PARTY_NOTICES.md)

[![WPF CI status](https://github.com/keida/codex-quota-float/actions/workflows/wpf-ci.yml/badge.svg?branch=main)](https://github.com/keida/codex-quota-float/actions/workflows/wpf-ci.yml)

The v1.1.3 release passed the WPF Release build and deterministic suite. The following remain explicitly **NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE**:

1. The same real Pro account `Fresh -> SignedOut -> Fresh`.
2. Isolated Codex `Present -> close -> three absence confirmations -> Quote Float response -> restart/recovery`.
3. Native Windows 125% / 150% DPI runtime acceptance.

## Repository layout and release history

- `wpf/`: WPF product implementation and tests.
- `docs/`: public technical records, contracts, acceptance evidence, and release records.
- `native/`: historical WinForms implementation kept for reference; it is not the current product implementation.

OpenAI, Codex, Microsoft, and Windows names identify compatibility targets; no affiliation or endorsement is implied. v1.1.3 is the current Latest Release; v1.1.2 is the prior published release, while the v1.1.1 tag is unpublished and superseded.
