# Roadmap

Codex Quota Float for Windows is currently available as the v1.1.2 Latest Release: a self-contained Windows x64 single-file EXE.

This is a public direction list, not a promise or schedule. Items are planned or under consideration and will be prioritized against evidence, maintenance cost, and user demand.

For the supporting discovery and launch work, see the [Growth Plan](docs/GROWTH-PLAN.md) and [Launch Kit](docs/LAUNCH-KIT.md). The launch material is a draft and has not been posted.

## Near-term discovery and trust

- **Planned — Demo GIF:** publish a short, truthful Full → edge snap → Orb → hover expansion → ZH/EN switch demonstration.
- **Planned — Social Preview:** create a 1280 × 640 or larger, under-1 MB preview image that leads with “Codex Quota Float for Windows”.
- **Planned — Code signing:** evaluate signing options for future Windows binaries. The current v1.1.2 EXE is unsigned.
- **Planned — Installation channels:** evaluate Scoop and WinGet packaging without implying support until a real package and update path exist.
- **Under consideration — Automatic updates:** define an update and rollback model before implementing any updater.

## Validation and product confidence

- **Planned — Native 125% / 150% DPI validation:** add real Windows runtime acceptance beyond the verified 100% / 96 DPI baseline.
- **Planned — Same-account lifecycle ticket:** validate the real Pro account Fresh → SignedOut → Fresh sequence.
- **Planned — Isolated Codex lifecycle ticket:** validate Codex close, absence confirmations, Quote Float response, restart, and recovery in isolation.

## Current boundary

The project remains Windows x64 and Codex-focused. The current release does not claim code signing, VirusTotal results, automatic updates, Scoop/WinGet support, or native 125% / 150% DPI acceptance. See the [Latest Release](https://github.com/keida/codex-quota-float/releases/tag/v1.1.2) and [technical acceptance record](docs/wpf-r1/ACCEPTANCE-SUMMARY.md) for the verified scope.
