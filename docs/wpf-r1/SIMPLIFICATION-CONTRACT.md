# WPF R1 Simplification Contract

Status: **implemented and accepted — Boss PASS**. Publication: **HOLD**.

This is the active R1 product contract. It supersedes pre-simplification controls and scale selectors; it does not authorize external publication.

## Settings

Settings is a fixed `420 × 296 DIP` non-modal WPF window at the verified Windows 100% / 96 DPI baseline. It has one `60 DIP` header drag surface. Language, refresh interval, and Done controls must not initiate a window drag.

The Settings user-facing controls are exactly:

- Language selection.
- Refresh interval selection: `15s`, `30s` default, `60s`, `2m`, `5m`.
- Done, which closes Settings.

Settings has no Refresh button and no sync/freshness status control. Saved-state and Auto Refresh text is read-only explanatory copy, not a setting or status control. Auto Refresh remains always ON. Removed from R1 are Usage navigation, Billing navigation and `BillingLauncher`, the Auto Refresh setting/control, Product Scale, Click-through, theme selection, Mini mode, display-mode selection, alert controls, and unrelated behavior toggles. Their handlers, strings, AutomationIds, layout, preferences, tests, and dead code are not part of the active contract.

## Full footer

The Full footer is separate from Settings and contains:

- Reset opportunity and reset timing information as read-only quota information.
- Sync/freshness status as read-only information; it is not a second refresh command.
- Refresh as the sole manual refresh command.
- The Settings entry.

## Widget state and presentation

`WidgetWindowController` owns Placement, TemporaryExpansion, and Plan. Display mode is derived: Free is Full; an edge placement with no temporary expansion is Orb; an edge placement with temporary expansion is Full. `ApplyState()` is the authoritative transaction for visual tree, dimensions, native bounds, edge anchor, hover, hit testing, DWM appearance, background, corner composition, and topmost ownership.

The verified baseline dimensions are:

| Plan | Full | Orb |
| --- | ---: | ---: |
| Plus | `278 × 216` | `74 × 84` |
| Pro | `278 × 156` | `74 × 62` |

Full-to-edge collapse, hover expansion, pointer-leave restoration, taskbar-safe work-area clamping, tray recovery, and single-instance activation are accepted interaction behavior. A normal competing window must remain below the widget when the accepted topmost state is active.

## Data and refresh

Loading without a prior accepted snapshot does not invent quota. Refreshing with an accepted snapshot retains that quota and renders `Refreshing` or `刷新中`; completion replaces it with the final state. Signed-out or session-changed data clears the old snapshot. Billing/Usage navigation is not an R1 surface.

## Evidence boundaries

This contract is supported by accepted static, build, deterministic-test, native smoke, and human interaction evidence. Static references do not prove runtime behavior. The verified native baseline is Windows 100% / 96 DPI only.

The following remain `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`:

1. Same real Pro account `Fresh -> SignedOut -> Fresh`.
2. Isolated Codex close, three absence confirmations, response, restart/recovery.
3. Native Windows 125% / 150% DPI runtime acceptance.
