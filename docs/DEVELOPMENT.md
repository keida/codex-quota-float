# Development

## Scope

The active R1 implementation is the WPF project under [`wpf/`](../wpf/). The native WinForms tree is a pre-existing dirty area and is not part of the WPF R1 publication set. The Tauri experiment is also outside R1 publication.

The accepted WPF state owner is `WidgetWindowController`; visual changes are applied through its unified `ApplyState()` transaction. `MainWindow` owns presentation, while the application coordinator owns single-instance activation and lifecycle coordination.

## Product rules

- Settings is non-modal, single-instance, and `420 × 296 DIP` at the verified 96 DPI baseline.
- Settings exposes language and refresh interval only, plus read-only sync/freshness status, Refresh, and Done.
- Billing/Usage navigation, Product Scale, Auto Refresh, Click-through, themes, and behavior selectors are removed from the R1 surface.
- Full/Orb geometry, edge/work-area anchoring, DWM corners, topmost ownership, tray recovery, and single-instance activation are separate acceptance concerns.
- Refreshing retains accepted quota; Loading is used only when no prior accepted quota exists.

## Evidence discipline

Static inspection, compilation, deterministic tests, native smoke, and human interaction are separate evidence levels. The accepted freeze evidence is reused here; this documentation freeze performs no product build or test run.

The focused test evidence is `31 fixtures; 58 checks; activeRequests=1; refreshCalls=1; backoffCalls=3; privacy=normalized-values-only`. The accepted Release build had zero warnings and zero errors. Native evidence is limited to the Windows 100% / 96 DPI baseline and the candidate identity in [`CANDIDATE-MANIFEST.json`](wpf-r1/CANDIDATE-MANIFEST.json).

Do not use real authentication or terminate user processes for deterministic tests. Do not record tokens, response bodies, cookies, account data, machine identifiers, PID/HWND values, or absolute personal paths in public artifacts.

## Explicit R1 limitations

1. Same real Pro account `Fresh -> SignedOut -> Fresh`: `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
2. Isolated Codex close/absence/response/restart recovery: `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
3. Native Windows 125% / 150% DPI runtime acceptance: `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.

Publication remains HOLD. Use the exact staging set in [`PUBLICATION-ALLOWLIST-V3.md`](wpf-r1/PUBLICATION-ALLOWLIST-V3.md); never stage native dirty files, output evidence, binaries, credentials, or local machine artifacts.
