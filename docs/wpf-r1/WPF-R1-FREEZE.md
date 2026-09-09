# WPF R1 Freeze Record

Freeze result: **PASS for documentation freeze; publication HOLD**.

## Frozen candidate

The accepted simplified WPF candidate is now locally committed on branch `codex/wpf-r1-simplification`. The complete source snapshot is recorded at the source snapshot commit below; later documentation-only updates preserve the publication record. No push, merge, tag, Release, or external publication is performed. It is identified by:

- SourceLink-independent clean-path EXE SHA-256 `FF3AFD7A3CDC748D0C4AF15C34C6C7F4533BCC53876869ABE49EF39713CC012F`
- SourceLink-independent clean-path WPF DLL SHA-256 `DF689E6ABB15B8099A96B81E18B133BBA581D83CCA8CB1371F060AB20505FBD9`
- Windows 100% / 96 DPI native baseline
- Current WPF source inventory in [`CANDIDATE-MANIFEST.json`](CANDIDATE-MANIFEST.json)

Complete source snapshot SHA is `e08353c8bfdbb1ccb23c65ca3799bb722132d501`. The branch base and immutable comparison base are canonical `origin/main` SHA `d86e21e01d3e1ed389cb53afa7f327184a345c3a`. The prior `ce9cc923ded971d8aa051ab8d4b69dc065051c8b` is a historical identical-tree baseline, not the complete source snapshot commit. The SourceLink-independent hashes supersede prior path-specific artifact hashes; InformationalVersion is `1.0.0` without SourceRevisionId and SourceLink revision embedding is disabled, so checkout path and documentation-only Git SHA changes do not alter the artifacts. Semantic IL and WPF resource hashes remain unchanged.

Boss accepted the simplification and final ApplyState ownership fix. The independent ownership recheck passed, and the Release artifacts now reproduce byte-for-byte across two distinct clean checkout paths and remain independent of later documentation-only Git SHA without embedded SourceLink revision payload. Focused tests passed with 31 fixtures and 58 checks; lightweight runtime smoke passed for Plus Full and Settings at 96 DPI with normal exit and zero residual process. No push, merge, tag, Release, or external GitHub mutation is performed.

## Frozen contract

The active contract is [`SIMPLIFICATION-CONTRACT.md`](SIMPLIFICATION-CONTRACT.md). It removes Product Scale, Billing/Usage navigation, Click-through, and unrelated settings selectors. Settings is `420 × 296 DIP`; language and refresh interval are the only user-selectable settings, Done closes the window, and the sole manual Refresh action is in the Full footer. Saved/explanatory notes are not a sync/freshness setting or status; Auto Refresh is always on. Full/Orb state is controller-owned; the verified native baseline is 96 DPI.

## Publication gate

Publication is HOLD. [`PUBLICATION-ALLOWLIST-V3.md`](PUBLICATION-ALLOWLIST-V3.md) is the exact locally committed publication scope, not merely a proposal. Its local staging and rebaseline commits are complete; it excludes native dirty work, output evidence, binaries, credentials, runtime state, machine identifiers, and superseded material. No push, merge, tag, Release, or external mutation was performed.

## Unverified scope

Same real Pro account Fresh/SignedOut/Fresh, isolated Codex close/absence/response/restart recovery, and native 125% / 150% DPI remain `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
