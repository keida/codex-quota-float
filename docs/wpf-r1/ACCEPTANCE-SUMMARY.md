# WPF R1 Acceptance Summary

Status: **BOSS PASS** for the simplified candidate. Publication: **HOLD**.

## Candidate identity

- Candidate executable SHA-256: `075A2BDB318C0FDAC70BE83DDE059DAAF1F51BABF34BB552442003B12596130A`
- Candidate WPF DLL SHA-256: `3B31E7773F2FC3C7ED06343564BBA3E07843B22162501DA274CFA87428F941D1`
- Verified native baseline: Windows 100% / 96 DPI.
- Source inventory and hashes: [`CANDIDATE-MANIFEST.json`](CANDIDATE-MANIFEST.json).

## Accepted evidence

- Release build: PASS, zero warnings and zero errors.
- Focused deterministic tests: PASS, `31 fixtures; 58 checks; activeRequests=1; refreshCalls=1; backoffCalls=3; privacy=normalized-values-only`.
- Targeted native smoke: Plus Full `278 × 216`, Plus Orb `74 × 84`, Pro Full `278 × 156`, Pro Orb `74 × 62`; effective DPI 96; Topmost true; DWM corner contract observed; no non-zero region; clean exit and no residual process.
- Single-instance activation: second launch exited cleanly, one process remained, and the first window reapplied the current state.
- Human ownership smoke: border/corners/no clipping, Full-to-edge Orb, short hover, long hover temporary Full, pointer-leave restoration, and topmost against a normal competing window — PASS.
- Independent source/ownership review: PASS; only `ApplyState()` owns the relevant Topmost and DWM scheduling paths, and activation routes through state reapplication.

The freeze task reused this accepted evidence and did not build or run product tests.

## Contract result

Settings is fixed at `420 × 296 DIP`. The only user-selectable settings are language and refresh interval; Done closes the window. Saved/explanatory notes are not a sync/freshness setting or status. Settings has no manual Refresh button; the sole manual Refresh action is in the Full footer. Auto Refresh is always on and may be shown as an explanatory note. Product Scale, Billing/Usage navigation, Click-through, theme selectors, and unrelated behavior toggles are removed from the active R1 contract. Full/Orb state is applied through `WidgetWindowController.ApplyState()`.

## Explicit limitations

1. Same real Pro account `Fresh -> SignedOut -> Fresh` — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
2. Isolated Codex `Present -> close -> three absence confirmations -> Quote Float response -> restart/recovery` — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
3. Native Windows 125% / 150% DPI runtime acceptance — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.

These limitations are not inferred as PASS from deterministic or simulated evidence.
