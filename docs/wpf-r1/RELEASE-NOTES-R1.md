# WPF R1 Release Notes — v1.1.0

Release state: **v1.1.0 released; Latest**.

The simplification source was merged by PR #2 into `main` at `2db7775723201206a23a12752752368f0a9a25ec`. v1.1.0 is tagged at `fc8e2749e7661b19509ee0bb924f04d204779420` and published as the Latest Release. The existing v1.0.0 tag and Draft Release remain historical and are not reused or modified.

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

The v1.1.0 release passed the Release build with zero warnings and errors in isolated clean tag worktrees; the EXE and WPF DLL were byte-identical:

- EXE SHA-256: `322DC1F8ED39A6769CCC1102A6C5D2CABBF088CF2C93003399D50C942CDCE507`
- WPF DLL SHA-256: `E5F50F66903C29F45AEFF799B6738254A8D6CEC0D59A508103B89EC68D250976`
- Assembly/File/Informational version: `1.1.0.0` / `1.1.0.0` / `1.1.0`
- Windows x64 framework-dependent package; `.NET 8 Desktop Runtime` required.
- ZIP SHA-256: `8B6D4AB34A39F9D466A499150968AEA4DDD344FAC1B33B5CDA1696AE5F40024D`
- `SHA256SUMS.txt` SHA-256: `E1AB8016C66F417AE003109981115798B761500C6AE796107F618EC5F0985A96`

The release also passed the 31-fixture/58-check deterministic suite, targeted native 96 DPI smoke, isolated extracted-package runtime smoke, single-instance activation smoke, and human ownership smoke. See [`ACCEPTANCE-SUMMARY.md`](ACCEPTANCE-SUMMARY.md) and [`CANDIDATE-MANIFEST.json`](CANDIDATE-MANIFEST.json).

## Limitations

The verified baseline is Windows 100% / 96 DPI. Same-account Fresh/SignedOut/Fresh, isolated Codex close/absence/response/restart recovery, and native Windows 125% / 150% DPI runtime acceptance remain `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.

The published package and checksums are available from the [v1.1.0 Latest Release](https://github.com/keida/codex-quota-float/releases/tag/v1.1.0). The historical v1.0.0 tag and Draft Release remain unchanged.
