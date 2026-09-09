# Quote Float

The WPF R1 candidate is simplified and frozen. Its status is **Boss PASS, publication HOLD**: source is recorded in the local rebaseline branch's Commit 1 and the documentation freeze is recorded by Commit 2; no push, merge, tag, Release, or external publication is performed.

## Current product contract

- Native WPF Civic Wayfinding widget for Plus and Pro plans.
- Full and Orb are derived display modes from placement, hover, and temporary expansion state.
- Settings is a fixed `420 × 296 DIP` non-modal window. The only user-selectable settings are language and refresh interval; Done closes the window. Saved/explanatory notes are not a settings status, and Settings has no Refresh button. The sole manual Refresh action is in the Full footer. Auto Refresh is always on and is shown only as an explanatory note.
- Refreshing retains accepted quota; without a prior snapshot the active request shows Loading.
- R1 excludes Product Scale, Billing/Usage navigation, the Auto Refresh toggle, Click-through, and theme/behavior selectors.

## Running and development

The WPF project is [`wpf/QuotaFloat.Wpf.csproj`](wpf/QuotaFloat.Wpf.csproj). Development guidance is in [`docs/DEVELOPMENT.md`](docs/DEVELOPMENT.md). Candidate source, hashes, deletions, and evidence boundaries are recorded in [`docs/wpf-r1/CANDIDATE-MANIFEST.json`](docs/wpf-r1/CANDIDATE-MANIFEST.json).

Real quota mode reads local Codex authentication and calls the ChatGPT usage/reset-credit services. Never publish authentication files, tokens, raw responses, account screenshots, or private diagnostics. `--direct` starts independently; `--watch` observes Codex presence. The application does not terminate Codex.

## Acceptance status

See [`docs/wpf-r1/ACCEPTANCE-SUMMARY.md`](docs/wpf-r1/ACCEPTANCE-SUMMARY.md) for the freeze evidence, [`docs/wpf-r1/SIMPLIFICATION-CONTRACT.md`](docs/wpf-r1/SIMPLIFICATION-CONTRACT.md) for the contract, and [`docs/wpf-r1/PUBLICATION-ALLOWLIST-V3.md`](docs/wpf-r1/PUBLICATION-ALLOWLIST-V3.md) for publication boundaries.

The verified baseline is Windows 100% / 96 DPI. These items remain explicitly `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`:

1. The same real Pro account `Fresh -> SignedOut -> Fresh`.
2. Isolated Codex `Present -> close -> three absence confirmations -> Quote Float response -> restart/recovery`.
3. Native Windows 125% / 150% DPI runtime acceptance.

Boss PASS does not grant external publication authorization. The V3 scope has been locally staged and committed; publication HOLD now means clean committed-tree verification, publication review, and separate external authorization remain.
