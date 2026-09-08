# WPF R1 acceptance summary

State: ACCEPTED WITH DOCUMENTED LIMITATIONS. Current source-detached candidate: PUB-WPF-001, accepted after QF-WPF-012. This summary describes prior observed results; publication cleanup step 2 ran only static/file/hash checks.

## Evidence provenance

QF-WPF-012 final independent review accepted the earlier candidate after builds, deterministic tests, Settings resource checks, English safe-state inspection, Minimal Refreshing, targeted scale/tray regression and cleanup. Its detailed final recheck is retained as a local audit record under `output/wpf-r1/`, not shipped.

PUB-WPF-001 then copied QuotaData.cs, QuotaClient.cs and CodexLifecycle.cs byte-for-byte into WPF-owned Shared files and removed the three native project links. Its returned build/test/runtime evidence and subsequent independent acceptance are the provenance of the current candidate. No separate on-disk migration evidence file is asserted.

## Current candidate and checks

- EXE SHA-256: `77A2E2D531C56E6B5FE2A0E69A4203DD1E667ED61D88BB7FFF80A952E3C11A40`.
- DLL SHA-256: `CADA7E944A418F080EA43658B6C76ACCE68E05824C050AA7A66C9F0F122256E1`.
- PUB-WPF-001 current-tree product/test Release builds: PASS, 0 warnings/errors.
- Focused deterministic suite: PASS, 29 fixtures; activeRequests=1, refreshCalls=1, backoffCalls=3, normalized-values-only output.
- Independent source view: 30 non-generated WPF source/project files, no native directory or native compile path; Release builds and 29 fixtures PASS.
- Real synthetic Full smoke on current candidate: 278 × 216, 96 DPI, process-path identity verified; automatic and normal exit both code 0, residual processes 0.
- Three copies matched the accepted native input bytes; all 17 protected native dirty-file hashes remained unchanged.
- Disposable source-view directory cleanup completed.

The earlier final-recheck DLL is superseded. Artifact hashes do not imply a tagged release, clean committed build or byte reproducibility across different roots. Full source and four runtime-file hashes are in [CANDIDATE-MANIFEST.json](CANDIDATE-MANIFEST.json).

## Reused acceptance and its limits

Unchanged UI/interaction code retains QF-WPF-012 accepted evidence: Plus/Pro, Chinese/English and 40/70/100 geometry at native 96 DPI; Full/Orb, hover, topmost, edge/work-area, Settings and tray recovery. The later shared-source migration reran affected builds/tests and lightweight Full exit smoke, not the whole UI/stress matrix.

The accepted Settings resource result after 50 warm-up, 150 measured cycles and 120-second cooldown was about +2.09 MiB, below the retained 10 MiB budget, with bounded handles/threads/windows and clean exit. This historical measurement is reused, not newly measured on the source-detached DLL.

Minimal Refreshing acceptance used the real coordinator and window with a controlled source: no prior snapshot remains Loading; prior accepted quota remains visible with Refreshing / 刷新中; completion shows Synced / 已同步. It does not establish real authentication recovery.

The 29 fixtures exercise controlled data and orchestration seams. They do not prove a live account round trip, notification delivery, or real Codex close/restart.

## Mandatory limitations

1. Same real Pro account `Fresh -> SignedOut -> Fresh` — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
2. Isolated Codex `Present -> close -> three absence confirmations -> Quote Float response -> restart/recovery` — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
3. Native Windows 125% / 120 DPI and 150% / 144 DPI runtime acceptance — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.

Native verified baseline: Windows 100% / 96 DPI.

## Evidence reuse

- Relevant code changed -> rerun the relevant heavy gate.
- Relevant code unchanged -> reuse accepted evidence plus lightweight smoke.

Reuse requires source/candidate hashes and changed-file analysis that covers dependencies, packaging and runtime configuration.

## Publication cleanup step 2

Static source/manifest, selection, size, privacy/path and protected-hash checks are recorded in [PUBLICATION-ALLOWLIST-V2.md](PUBLICATION-ALLOWLIST-V2.md). No build, test or application launch occurred in that documentation step. Acceptance does not imply commit, installation, release upload or publication approval.
