# Quote Float

**Language / 语言:** English (current) · [简体中文](README.zh-CN.md)

[![WPF CI status](https://github.com/keida/codex-quota-float/actions/workflows/wpf-ci.yml/badge.svg?branch=main)](https://github.com/keida/codex-quota-float/actions/workflows/wpf-ci.yml)

Quote Float is a native Windows WPF widget that keeps Codex Plus/Pro quota status visible in compact Full or Orb views.

![Quote Float WPF R1 actual WPF client-area renders](docs/assets/quote-float-overview-en.png)

_Figure: actual WPF client-area renders with illustrative sample quota values; native DWM border/corners are not captured._

The WPF R1 simplification is frozen and released. Its status is **Boss PASS, v1.1.2 RELEASED / LATEST**: the release is a self-contained Windows x64 single-file application and does not require a separate .NET installation.

## Quick start

1. Download the [v1.1.2 Windows x64 single-file EXE](https://github.com/keida/codex-quota-float/releases/download/v1.1.2/Quote-Float-v1.1.2-win-x64.exe) and [SHA256SUMS.txt](https://github.com/keida/codex-quota-float/releases/download/v1.1.2/SHA256SUMS.txt).
2. Run `Quote-Float-v1.1.2-win-x64.exe` directly. No separate .NET installation is required.
3. Optional: from the folder containing the downloaded EXE and `SHA256SUMS.txt`, verify the EXE with PowerShell:

   ```powershell
   $line = Get-Content .\SHA256SUMS.txt | Where-Object { $_ -match 'Quote-Float-v1.1.2-win-x64\.exe$' }
   $expected = ($line -split '\s+')[0]
   $actual = (Get-FileHash .\Quote-Float-v1.1.2-win-x64.exe -Algorithm SHA256).Hash
   if ($actual -ne $expected) { throw 'Checksum mismatch' }
   "SHA-256 OK: $actual"
   ```

## Current product contract

- Native WPF Civic Wayfinding widget for Plus and Pro plans.
- Full and Orb are derived display modes from placement, hover, and temporary expansion state.
- Settings is a fixed `420 × 296 DIP` non-modal window. The only user-selectable settings are language and refresh interval; Done closes the window. Saved/explanatory notes are not a settings status, and Settings has no Refresh button. The sole manual Refresh action is in the Full footer. Auto Refresh is always on and is shown only as an explanatory note.
- Refreshing retains accepted quota; without a prior snapshot the active request shows Loading.
- R1 excludes Product Scale, Billing/Usage navigation, the Auto Refresh toggle, Click-through, and theme/behavior selectors.

## Running and development

The WPF project is [`wpf/QuotaFloat.Wpf.csproj`](wpf/QuotaFloat.Wpf.csproj). Development guidance is in [`docs/DEVELOPMENT.md`](docs/DEVELOPMENT.md), and security reporting guidance is in [`SECURITY.md`](SECURITY.md). Current source, hashes, deletions, and evidence boundaries are recorded in [`docs/wpf-r1/CANDIDATE-MANIFEST.json`](docs/wpf-r1/CANDIDATE-MANIFEST.json).

Real quota mode reads local Codex authentication and calls the ChatGPT usage/reset-credit services. Never publish authentication files, tokens, raw responses, account screenshots, or private diagnostics. `--direct` starts independently; `--watch` observes Codex presence. The application does not terminate Codex.

## Repository layout

- `wpf/`: current WPF product implementation and WPF tests.
- `docs/`: public technical records, contracts, acceptance evidence summaries, and release records.
- `native/`: historical WinForms implementation kept for reference; it is not the current WPF product implementation.

The obsolete `design-preview/` design previews were removed; they are not current product screenshots or release assets.

## Acceptance status

See [`docs/wpf-r1/ACCEPTANCE-SUMMARY.md`](docs/wpf-r1/ACCEPTANCE-SUMMARY.md) for the freeze evidence, [`docs/wpf-r1/SIMPLIFICATION-CONTRACT.md`](docs/wpf-r1/SIMPLIFICATION-CONTRACT.md) for the contract, and [`docs/wpf-r1/PUBLICATION-ALLOWLIST-V3.md`](docs/wpf-r1/PUBLICATION-ALLOWLIST-V3.md) for publication boundaries.

The verified baseline is Windows 100% / 96 DPI. These items remain explicitly `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`:

1. The same real Pro account `Fresh -> SignedOut -> Fresh`.
2. Isolated Codex `Present -> close -> three absence confirmations -> Quote Float response -> restart/recovery`.
3. Native Windows 125% / 150% DPI runtime acceptance.

## Published release

- [v1.1.2 Latest Release](https://github.com/keida/codex-quota-float/releases/tag/v1.1.2)
- [Download the Windows x64 single-file EXE](https://github.com/keida/codex-quota-float/releases/download/v1.1.2/Quote-Float-v1.1.2-win-x64.exe)
- [SHA256SUMS.txt](https://github.com/keida/codex-quota-float/releases/download/v1.1.2/SHA256SUMS.txt)
- EXE SHA-256: `2FFF59F1B79E232C7ADBA88C49D57332ECA19EE2BE4FCC9E60195B5898D9E9DF`
- SHA256SUMS.txt SHA-256: `3E278A81B55341D57F8635B68A681F126110688E575A313B8E287FA30E7A968A`
- Product: self-contained Windows x64 single-file application; no separate .NET installation is required.
- Tray menu labels follow the current ZH/EN language.

The V3 source scope and v1.1.2 tray localization fix are merged. v1.1.0 remains the prior published release; the v1.1.1 tag is unpublished and superseded. The historical v1.0.0 tag and Draft Release remain unchanged.
