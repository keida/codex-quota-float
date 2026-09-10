# WPF R1 Acceptance Summary

Status: **BOSS PASS** for the simplified release. v1.1.0: **RELEASED / LATEST**.

## Candidate identity

- Released executable SHA-256: `322DC1F8ED39A6769CCC1102A6C5D2CABBF088CF2C93003399D50C942CDCE507`
- Released WPF DLL SHA-256: `E5F50F66903C29F45AEFF799B6738254A8D6CEC0D59A508103B89EC68D250976`
- Released ZIP SHA-256: `8B6D4AB34A39F9D466A499150968AEA4DDD344FAC1B33B5CDA1696AE5F40024D`
- SHA256SUMS.txt SHA-256: `E1AB8016C66F417AE003109981115798B761500C6AE796107F618EC5F0985A96`
- Tag: `v1.1.0` at `fc8e2749e7661b19509ee0bb924f04d204779420`; published latest release: [Quote Float WPF R1 v1.1.0](https://github.com/keida/codex-quota-float/releases/tag/v1.1.0)
- Package: [Windows x64 framework-dependent ZIP](https://github.com/keida/codex-quota-float/releases/download/v1.1.0/QuoteFloat-WPF-R1-v1.1.0-win-x64.zip); requires `.NET 8 Desktop Runtime`.
- Verified native baseline: Windows 100% / 96 DPI.
- Source inventory and hashes: [`CANDIDATE-MANIFEST.json`](CANDIDATE-MANIFEST.json).

## Accepted evidence

- Release build: PASS, zero warnings and zero errors.
- Focused deterministic tests: PASS, `31 fixtures; 58 checks; activeRequests=1; refreshCalls=1; backoffCalls=3; privacy=normalized-values-only`.
- Targeted native smoke: Plus Full `278 × 216`, Plus Orb `74 × 84`, Pro Full `278 × 156`, Pro Orb `74 × 62`; effective DPI 96; Topmost true; DWM corner contract observed; no non-zero region; clean exit and no residual process.
- Single-instance activation: second launch exited cleanly, one process remained, and the first window reapplied the current state.
- Human ownership smoke: border/corners/no clipping, Full-to-edge Orb, short hover, long hover temporary Full, pointer-leave restoration, and topmost against a normal competing window — PASS.
- Independent source/ownership review: PASS; only `ApplyState()` owns the relevant Topmost and DWM scheduling paths, and activation routes through state reapplication.

The freeze task reused this accepted evidence; the published package was then rebuilt and smoke-tested from isolated clean tag worktrees.

## Contract result

Settings is fixed at `420 × 296 DIP`. The only user-selectable settings are language and refresh interval; Done closes the window. Saved/explanatory notes are not a sync/freshness setting or status. Settings has no manual Refresh button; the sole manual Refresh action is in the Full footer. Auto Refresh is always on and may be shown as an explanatory note. Product Scale, Billing/Usage navigation, Click-through, theme selectors, and unrelated behavior toggles are removed from the active R1 contract. Full/Orb state is applied through `WidgetWindowController.ApplyState()`.

## Explicit limitations

1. Same real Pro account `Fresh -> SignedOut -> Fresh` — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
2. Isolated Codex `Present -> close -> three absence confirmations -> Quote Float response -> restart/recovery` — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
3. Native Windows 125% / 150% DPI runtime acceptance — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.

These limitations are not inferred as PASS from deterministic or simulated evidence.
