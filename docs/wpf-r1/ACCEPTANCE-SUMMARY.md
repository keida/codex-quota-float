# WPF R1 Acceptance Summary

Status: **BOSS PASS** for the simplified release. v1.1.2: **RELEASED / LATEST**.

## Candidate identity

- Released executable: [Quote-Float-v1.1.2-win-x64.exe](https://github.com/keida/codex-quota-float/releases/download/v1.1.2/Quote-Float-v1.1.2-win-x64.exe), 71,675,147 bytes, self-contained Windows x64 single-file; no separate .NET installation required.
- Released executable SHA-256: `2FFF59F1B79E232C7ADBA88C49D57332ECA19EE2BE4FCC9E60195B5898D9E9DF`
- SHA256SUMS.txt: [download](https://github.com/keida/codex-quota-float/releases/download/v1.1.2/SHA256SUMS.txt); SHA-256: `3E278A81B55341D57F8635B68A681F126110688E575A313B8E287FA30E7A968A`
- Tag: `v1.1.2` annotated object `b7cf6651a6f683d9e46ff0b2e7a51e26d4ab1877`, peeled to `8ea5e0c5d06c2141754af57ef9c9ab27edba04bc`; published latest release: [Quote Float WPF R1 v1.1.2](https://github.com/keida/codex-quota-float/releases/tag/v1.1.2)
- Verified native baseline: Windows 100% / 96 DPI.
- Source inventory and hashes: [`CANDIDATE-MANIFEST.json`](CANDIDATE-MANIFEST.json).

## Accepted evidence

- Release build: PASS, zero warnings and zero errors.
- Focused deterministic tests: PASS, `36 fixtures; 71 checks; activeRequests=1; refreshCalls=1; backoffCalls=4; privacy=normalized-values-only`.
- Targeted native smoke: Plus Full `278 × 216` and Pro Full `278 × 156` at effective DPI 96; direct/demo startup visible, safe single-instance behavior, clean exit, and no residual process.
- Single-instance activation: second launch exited cleanly, one process remained, and the first window reapplied the current state.
- Human interaction: prior WPF R1 border/corners, edge/hover/topmost behavior remained accepted; v1.1.2 ZH→EN→ZH tray menu localization audit — PASS.
- PR #12 WPF CI and GitGuardian checks — PASS.

The v1.1.2 package was published from the exact merged source commit above; release asset size, digest, and downloadability were independently verified after publication. v1.1.0 remains the prior published release, while v1.1.1 is an unpublished superseded tag.

## Contract result

Settings is fixed at `420 × 296 DIP`. The only user-selectable settings are language and refresh interval; Done closes the window. Saved/explanatory notes are not a sync/freshness setting or status. Settings has no manual Refresh button; the sole manual Refresh action is in the Full footer. Auto Refresh is always on and may be shown as an explanatory note. Product Scale, Billing/Usage navigation, Click-through, theme selectors, and unrelated behavior toggles are removed from the active R1 contract. Full/Orb state is applied through `WidgetWindowController.ApplyState()`.

## Explicit limitations

1. Same real Pro account `Fresh -> SignedOut -> Fresh` — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
2. Isolated Codex `Present -> close -> three absence confirmations -> Quote Float response -> restart/recovery` — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
3. Native Windows 125% / 150% DPI runtime acceptance — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.

These limitations are not inferred as PASS from deterministic or simulated evidence.
