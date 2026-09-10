# Development

## Scope

The active R1 implementation is the WPF project under [`wpf/`](../wpf/). The `native/` tree is a historical WinForms reference and is not part of the current WPF product or publication scope. The Tauri experiment is also outside R1 scope.

The accepted WPF state owner is `WidgetWindowController`; visual changes are applied through its unified `ApplyState()` transaction. `MainWindow` owns presentation, while the application coordinator owns single-instance activation and lifecycle coordination.

## Product rules

- Settings is non-modal, single-instance, and `420 × 296 DIP` at the verified 96 DPI baseline.
- Settings exposes language and refresh interval only. Done closes the window; saved, sync/freshness, and auto-refresh text is explanatory. Settings has no Refresh button; the sole manual Refresh action is in the Full footer.
- Billing/Usage navigation, Product Scale, Auto Refresh, Click-through, themes, and behavior selectors are removed from the R1 surface.
- Full/Orb geometry, edge/work-area anchoring, DWM corners, topmost ownership, tray recovery, and single-instance activation are separate acceptance concerns.
- Refreshing retains accepted quota; Loading is used only when no prior accepted quota exists.

## Evidence discipline

Static inspection, compilation, deterministic tests, native smoke, and human interaction are separate evidence levels. The accepted freeze evidence is reused here; this documentation freeze performs no product build or test run.

The focused test evidence is `31 fixtures; 58 checks; activeRequests=1; refreshCalls=1; backoffCalls=3; privacy=normalized-values-only`. The accepted Release build had zero warnings and zero errors. Native evidence is limited to the Windows 100% / 96 DPI baseline and the candidate identity in [`CANDIDATE-MANIFEST.json`](wpf-r1/CANDIDATE-MANIFEST.json).

Do not use real authentication or terminate user processes for deterministic tests. Do not record tokens, response bodies, cookies, account data, machine identifiers, PID/HWND values, or absolute personal paths in public artifacts.

## Build and test

Run these commands from the repository root with the .NET 8 SDK and Windows desktop targeting support installed:

```powershell
dotnet restore wpf/tests/QuotaFloat.Wpf.Tests.csproj
dotnet build wpf/QuotaFloat.Wpf.csproj --configuration Release --no-restore --nologo
dotnet build wpf/tests/QuotaFloat.Wpf.Tests.csproj --configuration Release --no-restore --nologo
dotnet run --project wpf/tests/QuotaFloat.Wpf.Tests.csproj --configuration Release --no-restore --no-build
```

## Explicit R1 limitations

1. Same real Pro account `Fresh -> SignedOut -> Fresh`: `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
2. Isolated Codex close/absence/response/restart recovery: `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
3. Native Windows 125% / 150% DPI runtime acceptance: `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.

Version 1.1.0 is published and is the latest release. For future publication, use the exact staging boundaries in [`PUBLICATION-ALLOWLIST-V3.md`](wpf-r1/PUBLICATION-ALLOWLIST-V3.md) and exclude credentials, machine-local state, generated output, release binaries, and unrelated historical or native changes from source commits.
