# WPF R1 Release Notes — v1.1.2

Release state: **v1.1.2 released; Latest**.

The v1.1.2 source is merged into `main` by PR #12 at `8ea5e0c5d06c2141754af57ef9c9ab27edba04bc`. The annotated `v1.1.2` tag object is `b7cf6651a6f683d9e46ff0b2e7a51e26d4ab1877` and peels to that merge commit. v1.1.0 remains the prior published release; v1.1.1 is an unpublished superseded tag. The existing v1.0.0 tag and Draft Release remain historical and are not reused or modified.

## Included

- Self-contained Windows x64 single-file desktop entry; no separate .NET installation is required.
- Native WPF Plus/Pro widget with accepted Full/Orb geometry and edge behavior.
- Settings simplification: fixed `420 × 296 DIP` non-modal window with language and refresh interval choices; Done closes it. Saved/explanatory notes are not a sync/freshness setting or status. There is no Settings Refresh button; the sole manual Refresh action is in the Full footer. Auto Refresh is always on and may be shown as an explanatory note.
- Minimal Refreshing behavior that retains accepted quota and does not invent data while Loading.
- Unified `WidgetWindowController.ApplyState()` ownership for presentation, native geometry, DWM corners, and topmost state.
- Tray recovery and single-instance activation with state reapplication.
- Cold-start visibility-grace reliability fix.
- Tray menu labels follow the current ZH/EN language and update with the selected language.

## Removed or superseded

- Product Scale and its former scale-specific layout contract.
- Billing/Usage navigation and `BillingLauncher`.
- Auto Refresh, Click-through, theme, Mini mode, display-mode, alert, and unrelated behavior selectors.
- `wpf/Windows/ProductScaleLayout.cs` and other obsolete source paths listed in the candidate manifest.

## Evidence and artifact

- Release build and WPF CI: PASS, zero warnings and zero errors; GitGuardian: PASS.
- Deterministic suite: `36 fixtures; 71 checks; activeRequests=1; refreshCalls=1; backoffCalls=4; privacy=normalized-values-only`.
- Human ZH→EN→ZH tray localization audit: PASS; native baseline remains Windows 100% / 96 DPI.
- Single-file EXE: [Quote-Float-v1.1.2-win-x64.exe](https://github.com/keida/codex-quota-float/releases/download/v1.1.2/Quote-Float-v1.1.2-win-x64.exe), 71,675,147 bytes.
- EXE SHA-256: `2FFF59F1B79E232C7ADBA88C49D57332ECA19EE2BE4FCC9E60195B5898D9E9DF`
- [SHA256SUMS.txt](https://github.com/keida/codex-quota-float/releases/download/v1.1.2/SHA256SUMS.txt) SHA-256: `3E278A81B55341D57F8635B68A681F126110688E575A313B8E287FA30E7A968A`

The published package and checksums are available from the [v1.1.2 Latest Release](https://github.com/keida/codex-quota-float/releases/tag/v1.1.2). See [`ACCEPTANCE-SUMMARY.md`](ACCEPTANCE-SUMMARY.md) and [`CANDIDATE-MANIFEST.json`](CANDIDATE-MANIFEST.json) for the acceptance record and current source inventory.

## Limitations

The verified baseline is Windows 100% / 96 DPI. The following remain `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`:

1. Same real Pro account `Fresh -> SignedOut -> Fresh`.
2. Isolated Codex close/absence/response/restart recovery.
3. Native Windows 125% / 150% DPI runtime acceptance.
