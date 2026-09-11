# WPF R1 Freeze Record

Freeze result: **PASS; v1.1.2 RELEASED / LATEST**.

## Frozen candidate

The accepted WPF source was merged by PR #12 into `main` at `8ea5e0c5d06c2141754af57ef9c9ab27edba04bc`. The annotated `v1.1.2` tag object is `b7cf6651a6f683d9e46ff0b2e7a51e26d4ab1877` and peels to that merge commit. v1.1.0 remains the prior published release; v1.1.1 is an unpublished superseded tag. The existing v1.0.0 tag and Draft Release remain historical and are not reused or modified.

It is identified by:

- Self-contained Windows x64 single-file EXE, 71,675,147 bytes, with no separate .NET installation required
- EXE SHA-256 `2FFF59F1B79E232C7ADBA88C49D57332ECA19EE2BE4FCC9E60195B5898D9E9DF`
- `SHA256SUMS.txt` SHA-256 `3E278A81B55341D57F8635B68A681F126110688E575A313B8E287FA30E7A968A`
- Assembly/Product/File version `1.1.2.0` / `1.1.2` / `1.1.2.0`
- Windows 100% / 96 DPI native baseline
- Current WPF source inventory in [`CANDIDATE-MANIFEST.json`](CANDIDATE-MANIFEST.json)

The source snapshot SHA, comparison base, and release identity are recorded in the current candidate manifest. Release build and WPF CI passed with zero warnings and zero errors; the deterministic suite passed with 36 fixtures and 71 checks. The v1.1.2 human ZH→EN→ZH tray localization audit passed, and the published asset digest and downloadability were verified after release.

## Frozen contract

The active contract is [`SIMPLIFICATION-CONTRACT.md`](SIMPLIFICATION-CONTRACT.md). It removes Product Scale, Billing/Usage navigation, Click-through, and unrelated settings selectors. Settings is `420 × 296 DIP`; language and refresh interval are the only user-selectable settings, Done closes the window, and the sole manual Refresh action is in the Full footer. Saved/explanatory notes are not a sync/freshness setting or status; Auto Refresh is always on. Full/Orb state is controller-owned; the verified native baseline is 96 DPI.

## Publication gate

Publication is COMPLETE for v1.1.2: [Latest Release](https://github.com/keida/codex-quota-float/releases/tag/v1.1.2). [`PUBLICATION-ALLOWLIST-V3.md`](PUBLICATION-ALLOWLIST-V3.md) remains the exact historical source-publication scope; release assets remain outside the repository source set. The allowlist excludes native dirty work, output evidence, binaries, credentials, runtime state, machine identifiers, and superseded material. The existing v1.0.0 tag and Draft Release are untouched.

## Unverified scope

The following remain `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`:

1. Same real Pro account Fresh/SignedOut/Fresh.
2. Isolated Codex close/absence/response/restart recovery.
3. Native Windows 125% / 150% DPI runtime acceptance.
