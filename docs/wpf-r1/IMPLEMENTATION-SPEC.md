# WPF R1 Implementation Specification

Status: **simplification implemented and Boss PASS**. Publication: **HOLD**.

## Authority and scope

This specification describes the current WPF R1 candidate. The active product contract is [`SIMPLIFICATION-CONTRACT.md`](SIMPLIFICATION-CONTRACT.md). Historical scale material is retained only in [`SCALE-CONTRACT.md`](SCALE-CONTRACT.md), which is superseded.

The candidate contains the WPF application, its WPF tests, and the WPF-owned copies of shared source required to build that application. It does not include the dirty native WinForms tree, Tauri material, local output, or release binaries.

## Presentation contract

At the verified Windows 100% / 96 DPI baseline:

| Plan | Full | Orb |
| --- | ---: | ---: |
| Plus | `278 × 216` | `74 × 84` |
| Pro | `278 × 156` | `74 × 62` |

Settings is a fixed `420 × 296 DIP` non-modal window. Its only user-selectable settings are language and refresh interval; Done closes the window. Saved/explanatory notes are read-only explanatory text, not a sync/freshness setting or status. Settings has no manual Refresh button: the sole manual Refresh action is in the Full footer. Auto Refresh is always on and may be shown as an explanatory note. Product Scale, Billing/Usage navigation, Click-through, themes, Mini mode, display-mode selectors, alerts, and unrelated behavior toggles are excluded from R1.

Full/Orb is derived from placement and temporary expansion. The controller is the sole authority for state application, visual dimensions, native bounds, edge anchoring, hover, hit testing, DWM appearance, corner composition, and topmost ownership. Source initialization, display/DPI changes, and single-instance activation reapply state through that authority.

## Data and lifecycle

Loading without an accepted prior snapshot does not invent quota. Refreshing retains an accepted snapshot and displays `Refreshing` or `刷新中`; completion replaces it with the final state. Signed-out or session-changed data clears the old snapshot. The application observes Codex presence and does not terminate Codex. `--direct` starts independently; `--watch` observes presence and exits after its accepted absence rule.

## Verification record

The accepted candidate record is [`ACCEPTANCE-SUMMARY.md`](ACCEPTANCE-SUMMARY.md), with hashes in [`CANDIDATE-MANIFEST.json`](CANDIDATE-MANIFEST.json). Accepted evidence includes:

- Release build: zero warnings and zero errors.
- Focused deterministic suite: `31 fixtures; 58 checks; activeRequests=1; refreshCalls=1; backoffCalls=3; privacy=normalized-values-only`.
- Native 96 DPI targeted smoke for Plus/Pro Full and Orb, with accepted geometry, DWM corner behavior, Topmost, clean exits, and no residual processes.
- Single-instance activation smoke with one remaining process and state reapplication.
- Human ownership smoke: Full border/corners, edge Orb geometry, short hover, long hover expansion, pointer-leave restoration, and topmost versus a normal competing window.

This freeze documentation reuses that evidence and performs no product build or test run. Static documents do not establish runtime behavior.

## Explicit exclusions

1. Same real Pro account `Fresh -> SignedOut -> Fresh`: `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
2. Isolated Codex close, three absence confirmations, response, restart/recovery: `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
3. Native Windows 125% / 150% DPI runtime acceptance: `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.

Publication remains HOLD until the exact V3 staging set is independently reviewed and separately authorized.
