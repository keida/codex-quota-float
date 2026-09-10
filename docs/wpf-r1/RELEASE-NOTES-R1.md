# WPF R1 Release Notes — v1.1.0

Release state: **v1.1.0 candidate prepared; publication HOLD**.

The simplification source was merged by PR #2 into `main` at `2db7775723201206a23a12752752368f0a9a25ec`. This v1.1.0 publication line records the version metadata and candidate artifacts on branch `codex/wpf-r1-v1.1.0-publication`; it does not publish a Release. The existing v1.0.0 tag and Draft Release remain historical and are not reused or modified.

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

The v1.1.0 candidate passed the Release build with zero warnings and errors in two distinct clean checkout paths; the EXE and WPF DLL were byte-identical:

- EXE SHA-256: `322DC1F8ED39A6769CCC1102A6C5D2CABBF088CF2C93003399D50C942CDCE507`
- WPF DLL SHA-256: `E5F50F66903C29F45AEFF799B6738254A8D6CEC0D59A508103B89EC68D250976`
- Assembly/File/Informational version: `1.1.0.0` / `1.1.0.0` / `1.1.0`

The candidate also passed the 31-fixture/58-check deterministic suite, targeted native 96 DPI smoke, single-instance activation smoke, and human ownership smoke. See [`ACCEPTANCE-SUMMARY.md`](ACCEPTANCE-SUMMARY.md) and [`CANDIDATE-MANIFEST.json`](CANDIDATE-MANIFEST.json).

## Limitations

The verified baseline is Windows 100% / 96 DPI. Same-account Fresh/SignedOut/Fresh, isolated Codex close/absence/response/restart recovery, and native Windows 125% / 150% DPI runtime acceptance remain `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.

No binary distribution, signing, tag movement, Draft Release mutation, or external Release publication is performed or authorized by these notes.
