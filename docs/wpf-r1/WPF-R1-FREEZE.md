# Quote Float WPF R1 Freeze

State: `PR #1 OPEN — READY FOR MERGE DECISION`
Acceptance: `QUOTE FLOAT WPF R1 — ACCEPTED WITH DOCUMENTED LIMITATIONS`  
Public evidence: [Acceptance summary](ACCEPTANCE-SUMMARY.md); historical final recheck retained locally, not shipped.

## Frozen candidate

- EXE: `wpf/bin/Release/net8.0-windows/QuotaFloat.Wpf.exe`
- EXE SHA-256: `77A2E2D531C56E6B5FE2A0E69A4203DD1E667ED61D88BB7FFF80A952E3C11A40`
- DLL: `wpf/bin/Release/net8.0-windows/QuotaFloat.Wpf.dll`
- DLL SHA-256: `CADA7E944A418F080EA43658B6C76ACCE68E05824C050AA7A66C9F0F122256E1`
- Native verified baseline: Windows 100% / 96 DPI.

These hashes identify the accepted local candidate after PUB-WPF-001 source detachment. The previous QF-WPF-012 DLL is superseded. PUB-WPF-001 reused accepted UI evidence and passed current-tree/independent-source-view builds, 29 fixtures and native 96 DPI smoke. This documentation step performed no fresh builds or runtime tests. Any later source, dependency, packaging, manifest, build-setting, or runtime-configuration change requires a new candidate identity and impact-based revalidation.

## Final ticket state

| Ticket | Final state |
|---|---|
| QF-WPF-001 | BOSS PASS |
| QF-WPF-002 | BOSS PASS |
| QF-WPF-003 | BOSS PASS |
| QF-WPF-004 | BOSS PASS |
| QF-WPF-004A | BOSS PASS |
| QF-WPF-005 | BOSS PASS |
| QF-WPF-006 | BOSS PASS |
| QF-WPF-007 | BOSS PASS |
| QF-WPF-008 | BOSS PASS |
| QF-WPF-009 | IMPLEMENTATION ACCEPTED / EXTERNAL ACCEPTANCE DEFERRED |
| QF-WPF-009E | DEFERRED / NOT VERIFIED — OUT OF R1 ACCEPTANCE SCOPE |
| QF-WPF-010 | BOSS PASS |
| QF-WPF-011 | BOSS PASS |
| QF-WPF-012F | BOSS PASS |
| QF-WPF-012 | BOSS PASS |

## R1 publication boundary

The exact selection is [PUBLICATION-ALLOWLIST-V2.md](PUBLICATION-ALLOWLIST-V2.md): 30 WPF source/project files, ten immutable canonical PNGs, and 13 public documentation/configuration files. Historical tickets, local audit records, agent state and generated binaries are omitted. Existing LICENSE remains tracked and unchanged. `THIRD_PARTY_NOTICES.md` is updated for WPF R1 and included in the exact selection. The current publication state is represented by open PR #1 from `codex/initial-publication` to `main`; no merge, tag or release has occurred.

PNG references preserve approved visual details. Later discrete-scale and Minimal Refreshing contracts override affected historical controls/statuses; static images are not final runtime captures.

## Protected legacy WinForms dirty files outside WPF R1

The following pre-existing `native/**` paths are not part of the WPF R1 candidate and must not be staged merely because WPF R1 is published:

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

These files remain user-owned dirty work. Do not reset, stash, clean, delete, overwrite, or include them in a WPF R1 commit without separate review and authorization.

## Final limitations

1. Same real Pro account `Fresh -> SignedOut -> Fresh` — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
2. Isolated Codex `Present -> close -> three absence confirmations -> Quote Float response -> restart/recovery` — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
3. Native Windows 125% / 120 DPI and 150% / 144 DPI runtime acceptance — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.

## Regression policy

- Relevant code changed -> rerun the relevant heavy gate.
- Relevant code unchanged -> reuse accepted evidence plus lightweight smoke.

Reuse must be justified by final manifest and changed-file analysis. This rule prevents redundant heavy lifecycle/stress reruns while preserving evidence validity.

## Publication sequence and current state

1. Review a precise staging allowlist and public privacy/size scan; keep all protected legacy WinForms paths excluded.
2. Create or confirm a dedicated release-preparation branch.
3. Commit WPF source/docs and an intentionally selected evidence set in reviewable commits.
4. Rebuild from the committed tree in a clean disposable worktree; compare source/candidate manifests and run only gates affected by packaging or source differences.
5. Push the release-preparation branch — completed for PR #1.
6. Open and review a pull request; PR #1 is open with the exact changed-file/privacy review and applicable check review completed.
7. Merge only after a separate merge decision; tag the accepted merge commit after the release version is explicitly chosen.
8. Build the release bundle from the tagged commit, publish SHA-256 checksums and the three limitations, then create the GitHub Release under separate release authorization.

Current state: PR #1 is open and ready for a merge decision. Merge, tag, bundle publication and GitHub Release creation remain separate decisions and have not occurred.
