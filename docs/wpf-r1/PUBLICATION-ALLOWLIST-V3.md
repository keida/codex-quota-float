# WPF R1 Publication Allowlist V3

Status: **exact local publication set; publication HOLD**.

Rebaseline branch: `codex/wpf-r1-simplification`. Complete source snapshot SHA: `1089a13bfb67549fabafedb83f11a022dfbf7784`. Comparison base: canonical `origin/main` at immutable SHA `d86e21e01d3e1ed389cb53afa7f327184a345c3a`. Expected staging set: **34 paths** — **20 WPF product/source/test paths**, **1 repository config path**, and **13 public documentation paths**. Optional public artifacts: **0**. Reproducible clean-path EXE/DLL hashes supersede prior path-specific artifact hashes; artifacts are independent of checkout path and subsequent documentation-only Git SHA; accepted runtime semantics and WPF resources are unchanged. No push, merge, tag, Release mutation, or external publication is authorized by this allowlist.

## A. PUBLICATION REQUIRED — WPF delta (20)

Modified or added WPF paths:

- `wpf/Diagnostics/EdgeDragDiagnostics.cs`
- `wpf/Interaction/OrbFullInteractionStateMachine.cs`
- `wpf/Platform/DwmWindowAppearance.cs`
- `wpf/Platform/WindowCornerContract.cs`
- `wpf/Platform/WindowPlacementService.cs`
- `wpf/Properties/AssemblyInfo.cs`
- `wpf/QuotaFloat.Wpf.csproj`
- `wpf/Resources/CivicWayfinding.xaml`
- `wpf/Resources/UiText.cs`
- `wpf/Services/PreferenceStore.cs`
- `wpf/Services/TrayIconService.cs`
- `wpf/Services/WpfApplicationCoordinator.cs`
- `wpf/Windows/MainWindow.xaml`
- `wpf/Windows/MainWindow.xaml.cs`
- `wpf/Windows/SettingsWindow.xaml`
- `wpf/Windows/SettingsWindow.xaml.cs`
- `wpf/Windows/WidgetWindowController.cs`
- `wpf/tests/Program.cs`

Deleted obsolete WPF paths:

- `wpf/Services/BillingLauncher.cs`
- `wpf/Windows/ProductScaleLayout.cs`

## A. PUBLICATION REQUIRED — repository config (1)

- `.gitignore`

## A. PUBLICATION REQUIRED — public documentation (13)

- `README.md`
- `README.en.md`
- `docs/DEVELOPMENT.md`
- `docs/wpf-r1/IMPLEMENTATION-SPEC.md`
- `docs/wpf-r1/TICKET-MAP.md`
- `docs/wpf-r1/ACCEPTANCE-SUMMARY.md`
- `docs/wpf-r1/CANDIDATE-MANIFEST.json`
- `docs/wpf-r1/RELEASE-NOTES-R1.md`
- `docs/wpf-r1/WPF-R1-FREEZE.md`
- `docs/wpf-r1/SIMPLIFICATION-CONTRACT.md`
- `docs/wpf-r1/SCALE-CONTRACT.md`
- `docs/wpf-r1/PUBLICATION-ALLOWLIST-V2.md`
- `docs/wpf-r1/PUBLICATION-ALLOWLIST-V3.md`

## B. OPTIONAL PUBLIC ARTIFACTS

None. Do not broaden the set with screenshots, PNGs, ZIPs, PDBs, logs, or local audit output.

## C. DO NOT PUBLISH

- All native dirty paths, including `native/UiTypography.cs`, `native/tests/InteractionRegression.cs`, `native/tests/RenderEvidence.cs`, `native/tests/UsageBillingRegression.cs`, and every modified native path.
- `output/`, `bin/`, `obj/`, cache, temporary, machine-state, task-state, or runtime-state paths.
- Credentials, `auth.json`, tokens, cookies, authorization headers, raw API payloads, account data, personal diagnostics, absolute personal paths, machine identifiers, PID/HWND values, Worker/task/thread identifiers, and private evidence logs.
- Tauri material, release binaries, EXE/DLL/ZIP/PDB files, screenshots, duplicate or superseded evidence, and any old scale PNG references.
- `PRODUCT.md` by default; it is not required by the WPF R1 delta.
- LICENSE/THIRD_PARTY or unrelated project files.

The deleted Billing and Product Scale source paths are explicit staging deletions. The current manifest is the source-hash authority; local output evidence is excluded.

## Required pre-publication checks

Before any push or separately authorized publication, re-check the exact 34-file committed range against canonical `origin/main` SHA `d86e21e01d3e1ed389cb53afa7f327184a345c3a`, confirm the index is empty after the local commits, verify canonical Git blob hashes and byte counts, scan the selected files for secrets/private paths/identifiers, validate Markdown links and JSON parsing, and confirm no native or output path is committed. The V3 scope has completed local staging and rebaseline commits; push, merge, tag, Release, and external publication remain unperformed.
