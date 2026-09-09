# WPF R1 Release Notes

Release state: **candidate frozen; publication HOLD**.

## Included

- Native WPF Plus/Pro widget with accepted Full/Orb geometry and edge behavior.
- Settings simplification: fixed `420 × 296 DIP` non-modal window with language and refresh interval choices; Done closes it. Saved/explanatory notes are not a sync/freshness setting or status. There is no Settings Refresh button; the sole manual Refresh action is in the Full footer. Auto Refresh is always on and may be shown as an explanatory note.
- Minimal Refreshing behavior that retains accepted quota and does not invent data while Loading.
- Unified `WidgetWindowController.ApplyState()` ownership for presentation, native geometry, DWM corners, and topmost state.
- Tray recovery and single-instance activation with state reapplication.

## Removed or superseded

- Product Scale and its former scale-specific layout contract.
- Billing/Usage navigation and `BillingLauncher`.
- Auto Refresh, Click-through, theme, Mini mode, display-mode, alert, and unrelated behavior selectors.
- `wpf/Windows/ProductScaleLayout.cs` and other obsolete source paths listed in the candidate manifest.

## Evidence

The accepted candidate passed the Release build, 31-fixture/58-check deterministic suite, targeted native 96 DPI smoke, single-instance activation smoke, and human ownership smoke. See [`ACCEPTANCE-SUMMARY.md`](ACCEPTANCE-SUMMARY.md) and [`CANDIDATE-MANIFEST.json`](CANDIDATE-MANIFEST.json).

## Limitations

The verified baseline is Windows 100% / 96 DPI. Same-account Fresh/SignedOut/Fresh, isolated Codex close/absence/response/restart recovery, and native Windows 125% / 150% DPI runtime acceptance remain `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.

No binary distribution, signing, version assignment, tag movement, Draft Release mutation, or external publication is performed or authorized by these notes.
