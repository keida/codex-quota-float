# WPF R1 publication allowlist V2

Publication cleanup step 2.5, revision 1. Exact proposed staging selection: 53 files in three reviewable commits. No staging or publication action is executed by this document.

## PUBLICATION REQUIRED — commit 1: WPF source and tests (30)

- `wpf/App.xaml`
- `wpf/App.xaml.cs`
- `wpf/Interaction/OrbFullInteractionStateMachine.cs`
- `wpf/Platform/WindowPlacementService.cs`
- `wpf/QuotaFloat.Wpf.csproj`
- `wpf/Resources/CivicWayfinding.xaml`
- `wpf/Resources/UiText.cs`
- `wpf/Services/BillingLauncher.cs`
- `wpf/Services/CodexPresenceService.cs`
- `wpf/Services/PreferenceStore.cs`
- `wpf/Services/QuotaRefreshCoordinator.cs`
- `wpf/Services/QuotaState.cs`
- `wpf/Services/SingleInstanceService.cs`
- `wpf/Services/TrayIconService.cs`
- `wpf/Services/WpfApplicationCoordinator.cs`
- `wpf/Shared/CodexLifecycle.cs`
- `wpf/Shared/QuotaClient.cs`
- `wpf/Shared/QuotaData.cs`
- `wpf/SharedUsings.cs`
- `wpf/Skins/CivicWayfinding/CivicWayfindingSkin.cs`
- `wpf/Skins/IWidgetSkin.cs`
- `wpf/Skins/WidgetSkinRegistry.cs`
- `wpf/Windows/MainWindow.xaml`
- `wpf/Windows/MainWindow.xaml.cs`
- `wpf/Windows/ProductScaleLayout.cs`
- `wpf/Windows/SettingsWindow.xaml`
- `wpf/Windows/SettingsWindow.xaml.cs`
- `wpf/app.manifest`
- `wpf/tests/Program.cs`
- `wpf/tests/QuotaFloat.Wpf.Tests.csproj`

## PUBLICATION REQUIRED — commit 2: canonical design references (10)

- `design-preview/wpf-r1/01-civic-full-orb-zh.png`
- `design-preview/wpf-r1/02-civic-full-orb-en.png`
- `design-preview/wpf-r1/03-civic-settings-zh.png`
- `design-preview/wpf-r1/04-civic-settings-en.png`
- `design-preview/wpf-r1/scale-approval/qf-wpf-010-scale-design-approval-pack.png`
- `design-preview/wpf-r1/scale-approval/candidate-a-40-orb-first.png`
- `design-preview/wpf-r1/scale-approval/candidate-a-plus-70-zh.png`
- `design-preview/wpf-r1/scale-approval/candidate-a-plus-70-en.png`
- `design-preview/wpf-r1/scale-approval/candidate-a-pro-70-zh.png`
- `design-preview/wpf-r1/scale-approval/candidate-a-pro-70-en.png`

Retain these ten files byte-for-byte. They are static design references, not executable downloads or final native captures. Later discrete 40/70/100 and Minimal Refreshing contracts supersede affected historical controls/statuses. The main approval pack retains a historical Candidate B comparison; B is rejected. Separate B images, ZIPs, duplicate aliases, detail crops and render scripts are excluded.

## PUBLICATION REQUIRED — commit 3: public docs and notices (13)

- `.gitignore`
- `README.md`
- `README.en.md`
- `docs/DEVELOPMENT.md`
- `docs/wpf-r1/IMPLEMENTATION-SPEC.md`
- `docs/wpf-r1/SCALE-CONTRACT.md`
- `docs/wpf-r1/TICKET-MAP.md`
- `docs/wpf-r1/WPF-R1-FREEZE.md`
- `docs/wpf-r1/RELEASE-NOTES-R1.md`
- `docs/wpf-r1/ACCEPTANCE-SUMMARY.md`
- `docs/wpf-r1/CANDIDATE-MANIFEST.json`
- `docs/wpf-r1/PUBLICATION-ALLOWLIST-V2.md`
- `THIRD_PARTY_NOTICES.md`

`LICENSE` remains tracked and unchanged; do not stage it for this packet. `THIRD_PARTY_NOTICES.md` is included in this commit and retains the upstream reference, revision, links, attribution and complete MIT text. The READMEs still contain the earlier warning that notice wording requires a separate follow-up; README edits are outside this packet, so that stale warning is a publication blocker requiring a later authorized README correction.

## PUBLICATION OPTIONAL

No additional optional Git files are approved for this release selection.

Only after separate release authorization, rebuild/package and review these runtime assets; never stage the generated copies in Git:

- `wpf/bin/Release/net8.0-windows/QuotaFloat.Wpf.exe`
- `wpf/bin/Release/net8.0-windows/QuotaFloat.Wpf.dll`
- `wpf/bin/Release/net8.0-windows/QuotaFloat.Wpf.deps.json`
- `wpf/bin/Release/net8.0-windows/QuotaFloat.Wpf.runtimeconfig.json`

[CANDIDATE-MANIFEST.json](CANDIDATE-MANIFEST.json) records current hashes, not an authorization to distribute them or a claim of a fresh tagged build.

## DO NOT PUBLISH — protected native dirty files (17)

- `native/CodexLifecycle.cs`
- `native/Preferences.cs`
- `native/Program.cs`
- `native/QuotaData.cs`
- `native/QuotaFloat.csproj`
- `native/QuotaForm.cs`
- `native/SettingsForm.cs`
- `native/tests-lifecycle/Program.cs`
- `native/tests-lifecycle/host/HostProgram.cs`
- `native/tests/FollowRegression.cs`
- `native/tests/Program.cs`
- `native/tests/ResizeSnapRegression.cs`
- `native/tests/WindowRegression.cs`
- `native/UiTypography.cs`
- `native/tests/InteractionRegression.cs`
- `native/tests/RenderEvidence.cs`
- `native/tests/UsageBillingRegression.cs`

This exclusion applies to the current dirty changes; it does not remove existing tracked legacy history. Preserve all these files locally.

## DO NOT PUBLISH — other paths and patterns

- `PRODUCT.md`, `docs/UI-SPEC-R12.md`, `docs/wpf-r1/tickets/**`.
- `.d-ai/**`, `.codex/**`, `.impeccable/**`, `.vs/**`.
- `output/**`, `release/**`, `prototype/**`, `native/test-output/**`.
- All `bin/`, `obj/`, test-result, cache, temporary and local runtime/diagnostic outputs.
- All design-preview files outside the exact ten-file selection above, including duplicate PNG aliases, B-only images, detail crops, ZIPs and render scripts.
- `auth.json`, `.env*`, token/cookie/session material, raw API responses, private screenshots, signing keys/certificates and machine-local preferences.
- Superseded evidence, large logs, snapshots, temporary harness binaries, PID/HWND logs and absolute personal paths.

Ignore rules protect local files; they do not untrack historical content or authorize deletion. The exact per-commit lists above govern future staging. Do not stage entire directories or force-add excluded files.

## Size and privacy review

The selection consists of 30 source files, ten images and 13 text/configuration files. Selected files at least 1 MiB: none; greater than 5 MiB: none; greater than 10 MiB: none. Generated EXE/DLL and JSON runtime companions are Release assets only after authorization. ZIPs are excluded. All ten selected PNGs are below 1 MiB; no raw runtime screenshot is selected.

High-signal text review covers both edited files and the selected text/configuration and source files: personal absolute paths, email addresses, credential-shaped literal values and private task identifiers. Final scans found zero exposed values or personal paths. External URLs in the notice were checked for valid HTTPS URL syntax and point to the stated upstream project/revision; upstream content and licensing were not independently re-audited here. No secret values are copied into the public documents.

Selection checks: 53 exact files, zero ignored selected paths; 13 exclusion samples all ignored; 33 local Markdown links all resolve within the selected or retained files. Largest selected file: the scale approval pack, 225,892 bytes. The final link and ignore checks were recomputed after adding THIRD_PARTY_NOTICES.md.

PNG checks cover exact selection, byte size and unchanged hashes. This text scan cannot detect secrets embedded in image pixels or establish redistribution rights. The images are retained approved static references; no new visual/OCR or licensing audit is claimed here.

## Matching Result Packet

Step: PUBLICATION PREP STEP 2.5
Revision: 1
Status: NEEDS CLEANUP for actual publication; notice correction PASS, stale README warning remains a publication blocker
Files changed: `THIRD_PARTY_NOTICES.md` and this allowlist only; commit 3 now contains the exact 13 paths above.
Checks RAN: notice preservation review, upstream URL syntax review, candidate hashes, source/image protection, ignore selection, public links, privacy/path and size review.
Checks NOT RUN: build, automated runtime fixtures, application launch, native capture, heavy lifecycle/stress tests.
Prior evidence: PUB-WPF-001 accepted migration builds, 29 fixtures and native smoke are reused; no migration evidence file is invented.
Protection: 44 protected manifest/runtime/image entries compared with zero mismatches: 30 WPF source/project files, four candidate runtime files and the canonical ten PNGs. Three Shared/native copy hashes remain equal. LICENSE, all other product/source/design files and the 17 protected native dirty files remain unchanged. The notice baseline changed only under this packet; the allowlist baseline changed only to add the notice and update this result.
Git: index empty; HEAD unchanged. No commit/push/merge/tag/release action.
Limits: three R1 exclusions remain explicit in RELEASE-NOTES-R1.md and ACCEPTANCE-SUMMARY.md; README correction remains required before actual publication.
Next: independent review of this documentation selection. No step 3 action is authorized.

## R1 limitations

1. Same real Pro account `Fresh -> SignedOut -> Fresh` — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
2. Isolated Codex `Present -> close -> three absence confirmations -> Quote Float response -> restart/recovery` — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
3. Native Windows 125% / 120 DPI and 150% / 144 DPI runtime acceptance — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.

Native verified baseline: Windows 100% / 96 DPI.

- Relevant code changed -> rerun the relevant heavy gate.
- Relevant code unchanged -> reuse accepted evidence plus lightweight smoke.

Reuse requires source/candidate hashes and changed-file analysis that covers dependencies, packaging and runtime configuration.
