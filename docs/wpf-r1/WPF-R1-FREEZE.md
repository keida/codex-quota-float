# WPF R1 Freeze Record

Freeze result: **PASS; v1.1.0 RELEASED / LATEST**.

## Frozen candidate

The accepted simplified WPF source was merged by PR #2 into `main` at `2db7775723201206a23a12752752368f0a9a25ec`. The complete versioned source snapshot is recorded at the source snapshot commit below, and later documentation-only updates preserve the artifact record. v1.1.0 is tagged at `fc8e2749e7661b19509ee0bb924f04d204779420` and published as the [Latest Release](https://github.com/keida/codex-quota-float/releases/tag/v1.1.0). The existing v1.0.0 tag and Draft Release remain historical and are not reused or modified. It is identified by:

- Versioned clean-path EXE SHA-256 `322DC1F8ED39A6769CCC1102A6C5D2CABBF088CF2C93003399D50C942CDCE507`
- Versioned clean-path WPF DLL SHA-256 `E5F50F66903C29F45AEFF799B6738254A8D6CEC0D59A508103B89EC68D250976`
- Assembly/File/Informational version `1.1.0.0` / `1.1.0.0` / `1.1.0`
- Windows 100% / 96 DPI native baseline
- Current WPF source inventory in [`CANDIDATE-MANIFEST.json`](CANDIDATE-MANIFEST.json)

Complete versioned source snapshot SHA is `cb670afbea1391a175e6af1d9f58c4dc903d2988`. The branch base and immutable comparison base are canonical `origin/main` SHA `2db7775723201206a23a12752752368f0a9a25ec`. The v1.0.0 tag remains a historical pre-version-prep record, not the v1.1.0 candidate. The artifacts have no SourceRevisionId, SourceLink revision embedding is disabled, and later documentation-only Git SHA changes do not alter them. Semantic IL and WPF resource hashes remain unchanged.

Boss accepted the simplification and final ApplyState ownership fix. The independent ownership recheck passed, and the v1.1.0 Release artifacts reproduce byte-for-byte across isolated clean tag worktrees without embedded SourceLink revision payload. Focused tests passed with 31 fixtures and 58 checks; extracted-package runtime smoke passed for Plus Full and Settings at 96 DPI with normal exit, tray cleanup, and zero residual process. The source simplification is merged, and v1.1.0 is published as Latest. The historical v1.0.0 tag and Draft Release remain untouched.

## Frozen contract

The active contract is [`SIMPLIFICATION-CONTRACT.md`](SIMPLIFICATION-CONTRACT.md). It removes Product Scale, Billing/Usage navigation, Click-through, and unrelated settings selectors. Settings is `420 × 296 DIP`; language and refresh interval are the only user-selectable settings, Done closes the window, and the sole manual Refresh action is in the Full footer. Saved/explanatory notes are not a sync/freshness setting or status; Auto Refresh is always on. Full/Orb state is controller-owned; the verified native baseline is 96 DPI.

## Publication gate

Publication is COMPLETE for v1.1.0. [`PUBLICATION-ALLOWLIST-V3.md`](PUBLICATION-ALLOWLIST-V3.md) remains the exact historical source-publication scope; the v1.1.0 version metadata and release records were published separately, while release assets remain outside the repository source set. The allowlist excludes native dirty work, output evidence, binaries, credentials, runtime state, machine identifiers, and superseded material. The existing v1.0.0 tag and Draft Release are untouched.

## Unverified scope

Same real Pro account Fresh/SignedOut/Fresh, isolated Codex close/absence/response/restart recovery, and native 125% / 150% DPI remain `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
