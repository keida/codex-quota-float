# Quote Float

**Language / 语言:** [中文](README.md) · English (current)

[![WPF CI status](https://github.com/keida/codex-quota-float/actions/workflows/wpf-ci.yml/badge.svg?branch=main)](https://github.com/keida/codex-quota-float/actions/workflows/wpf-ci.yml)

The WPF R1 simplification is frozen and released. Its status is **Boss PASS, v1.1.0 RELEASED / LATEST**: the release is a Windows x64 framework-dependent package and requires the `.NET 8 Desktop Runtime`.

## Current product contract

- Native WPF Civic Wayfinding widget for Plus and Pro plans.
- Full and Orb are derived display modes from placement, hover, and temporary expansion state.
- Settings is a fixed `420 × 296 DIP` non-modal window. The only user-selectable settings are language and refresh interval; Done closes the window. Saved/explanatory notes are not a settings status, and Settings has no Refresh button. The sole manual Refresh action is in the Full footer. Auto Refresh is always on and is shown only as an explanatory note.
- Refreshing retains accepted quota; without a prior snapshot the active request shows Loading.
- R1 excludes Product Scale, Billing/Usage navigation, the Auto Refresh toggle, Click-through, and theme/behavior selectors.

## Running and development

The WPF project is [`wpf/QuotaFloat.Wpf.csproj`](wpf/QuotaFloat.Wpf.csproj). Development guidance is in [`docs/DEVELOPMENT.md`](docs/DEVELOPMENT.md). Current source, hashes, deletions, and evidence boundaries are recorded in [`docs/wpf-r1/CANDIDATE-MANIFEST.json`](docs/wpf-r1/CANDIDATE-MANIFEST.json).

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

- [v1.1.0 Release](https://github.com/keida/codex-quota-float/releases/tag/v1.1.0)
- [Download the Windows x64 ZIP](https://github.com/keida/codex-quota-float/releases/download/v1.1.0/QuoteFloat-WPF-R1-v1.1.0-win-x64.zip)
- [SHA256SUMS.txt](https://github.com/keida/codex-quota-float/releases/download/v1.1.0/SHA256SUMS.txt)
- EXE SHA-256: `322DC1F8ED39A6769CCC1102A6C5D2CABBF088CF2C93003399D50C942CDCE507`
- WPF DLL SHA-256: `E5F50F66903C29F45AEFF799B6738254A8D6CEC0D59A508103B89EC68D250976`
- ZIP SHA-256: `8B6D4AB34A39F9D466A499150968AEA4DDD344FAC1B33B5CDA1696AE5F40024D`

The V3 source scope has been merged, and the v1.1.0 tag and Latest Release are published. The historical v1.0.0 tag and Draft Release remain unchanged.
