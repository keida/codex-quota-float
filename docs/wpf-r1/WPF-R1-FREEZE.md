# WPF R1 Freeze Record

Freeze result: **PASS for documentation freeze; publication HOLD**.

## Frozen candidate

The accepted simplified WPF candidate is now locally committed on branch `codex/wpf-r1-simplification`. The complete source snapshot is recorded at the source snapshot commit below; later documentation-only updates preserve the publication record. No push, merge, tag, Release, or external publication is performed. It is identified by:

- EXE SHA-256 `075A2BDB318C0FDAC70BE83DDE059DAAF1F51BABF34BB552442003B12596130A`
- WPF DLL SHA-256 `3B31E7773F2FC3C7ED06343564BBA3E07843B22162501DA274CFA87428F941D1`
- Windows 100% / 96 DPI native baseline
- Current WPF source inventory in [`CANDIDATE-MANIFEST.json`](CANDIDATE-MANIFEST.json)

Complete source snapshot SHA is `def17bee8088ccf574b95c39b6f47802927504b0`. The branch base and immutable comparison base are canonical `origin/main` SHA `d86e21e01d3e1ed389cb53afa7f327184a345c3a`. The prior `ce9cc923ded971d8aa051ab8d4b69dc065051c8b` is a historical identical-tree baseline, not the complete source snapshot commit.

Boss accepted the simplification and final ApplyState ownership fix. The independent ownership recheck also passed. The local commit sequence records the WPF source candidate first and documentation-only updates afterward; no build, test, push, merge, tag, Release, or external GitHub mutation is performed.

## Frozen contract

The active contract is [`SIMPLIFICATION-CONTRACT.md`](SIMPLIFICATION-CONTRACT.md). It removes Product Scale, Billing/Usage navigation, Click-through, and unrelated settings selectors. Settings is `420 × 296 DIP`; language and refresh interval are the only user-selectable settings, Done closes the window, and the sole manual Refresh action is in the Full footer. Saved/explanatory notes are not a sync/freshness setting or status; Auto Refresh is always on. Full/Orb state is controller-owned; the verified native baseline is 96 DPI.

## Publication gate

Publication is HOLD. [`PUBLICATION-ALLOWLIST-V3.md`](PUBLICATION-ALLOWLIST-V3.md) is the exact locally committed publication scope, not merely a proposal. Its local staging and rebaseline commits are complete; it excludes native dirty work, output evidence, binaries, credentials, runtime state, machine identifiers, and superseded material. No push, merge, tag, Release, or external mutation was performed.

## Unverified scope

Same real Pro account Fresh/SignedOut/Fresh, isolated Codex close/absence/response/restart recovery, and native 125% / 150% DPI remain `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
