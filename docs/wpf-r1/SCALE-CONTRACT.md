# QF-WPF-010 — Accepted Scale Contract

State: APPROVED — CANDIDATE A  
Scope: Civic Wayfinding WPF R1 product scale at 40%, 70%, and 100%  
Product code status: IMPLEMENTED AND ACCEPTED at native 96 DPI
Approval record: `QF-WPF-010 SCALE CONTRACT — PASS` (2026-09-07)

## Historical design decision

The approved WPF R1 references define only the 100% Full and Orb states at 96 DPI. They do not define 70% or 40% outer bounds, responsive reflow, typography floors, compact footer treatment, or whether product scale changes Orb.

`docs/UI-SPEC-R12.md` cannot fill this gap. `IMPLEMENTATION-SPEC.md` explicitly classifies it as historical input, and its three-skin model, 40/65/100/180 breakpoints, Full dimensions, and 560 × 580 Settings window conflict with the current single-skin WPF R1 contract.

Therefore 40% and 70% are separately defined visual states. The user approved Candidate A on 2026-09-07. This document is now the canonical implementation contract for QF-WPF-010; Candidate B is rejected and has no implementation authority.

## Independent scale systems

**Product scale** selects a Quote Float logical layout state. It may change the Full window's logical bounds and reflow, but it must not be implemented as a bitmap or uniform text reduction.

**Windows DPI scale** maps the selected logical WPF layout from device-independent pixels to physical monitor pixels. It must not select, replace, or reinterpret the product layout state.

For an approved logical dimension `L` at effective DPI `D`, the expected physical dimension is checked as `round(L × D / 96)`, subject only to documented native window rounding. Product scale is applied first by selecting its approved logical layout; Windows DPI conversion is measured separately afterward.

Product Scale is a three-value discrete selector: `40%`, `70%`, or `100%`. No arbitrary integer value, intermediate percentage, or value above 100% is part of the R1 product contract. Persisted legacy or invalid values must be normalized to one of these three values before display and layout application, so the setting shown to the user always names the layout actually in use.

The R1 native acceptance matrix combines all three Product Scale states with Windows `100% / 96 DPI`. Every row records selected product state, logical bounds, physical bounds, effective DPI, monitor/work area, language, plan, and mode.

The retained Per-Monitor-V2, DPI-aware implementation and logical-DIP architecture remain in place; Windows `125% / 120 DPI` and `150% / 144 DPI` runtime acceptance remain `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`. Calculated or simulated dimensions may support engineering analysis only; they must not be reported as native PASS. QF-WPF-012 must preserve this limitation in final acceptance.

## State table

| Product scale | Plan | Full logical bounds | Orb logical bounds | Settings logical bounds | Contract status |
|---:|---|---:|---:|---:|---|
| 100% | Plus | 278 × 216 | 74 × 84 | 520 × 520 | Approved and frozen |
| 100% | Pro | 278 × 156 | 74 × 62 | 520 × 520 | Approved and frozen |
| 70% | Plus | 236 × 178 Compact Full | 74 × 84 | 520 × 520 | Approved and frozen |
| 70% | Pro | 236 × 128 Compact Full | 74 × 62 | 520 × 520 | Approved and frozen |
| 40% | Plus | none; Orb-first | 74 × 84 | 520 × 520 | Approved and frozen |
| 40% | Pro | none; Orb-first | 74 × 62 | 520 × 520 | Approved and frozen |

Settings does not participate in product scale. It remains 520 × 520 logical units at all product-scale selections, while Windows DPI still maps those logical units to physical pixels.

## Frozen content and visual invariants

These rules apply at every approved product scale:

- Plus renders each server-returned 5-hour and Weekly window; Pro reference state renders Weekly only. Missing server windows create no empty row.
- Every visible quota window retains its percentage, label, remaining/reset information, and independent right-pointing arrow-chain indicator.
- Reset opportunity count and earliest valid future expiry remain available.
- The Full bottom action area retains Billing, Sync, Refresh, and Settings. Sync remains a state; Refresh remains an action. Refresh and Settings retain visible text in the active language.
- Loading must not show invented quota; stale must not look fresh; signed-out and recovery states remain truthful.
- The Civic navy/amber system, solid outer border, slim title band, asymmetric approved window geometry, native Windows font families, and complete separate ZH/EN layouts remain unchanged.
- Restoring 100% must immediately apply and reproduce the accepted 100% layout and dimensions without accumulated drift; updating only the saved/displayed value is not sufficient.

At 40%, idle is the approved fixed-size Orb. Required Full information remains available through the approved 300 ms hover expansion to the corresponding 70% Compact Full; there is no independent 40% Full layout.

The application records why Orb is active. If selecting 40% forced a previously resting Full into Orb, changing Product Scale to 70% or 100% immediately restores the corresponding Compact Full or Full. If Orb was explicitly selected by the user, created by the accepted edge behavior, or is otherwise an existing interaction state independent of Product Scale, changing Product Scale must not force it open. Temporary hover expansion never changes the persisted Orb reason.

## Interaction, edge, and topmost invariants

These accepted behaviors remain independent of product scale:

- Product Always-On-Top remains enabled according to the accepted setting; capture-only TOPMOST is not product evidence.
- Edge detection continues to use the accepted 24 logical-pixel work-area threshold.
- Orb hover waits 300 ms before temporary Full expansion; cancellation before the threshold remains effective.
- At 100%, Orb hover expands to the accepted 100% Full. At 70%, Orb hover expands to the corresponding 70% Compact Full. At 40%, Orb-first hover also expands to the corresponding 70% Compact Full.
- Temporary Full expands toward the monitor work-area interior and returns to the exact saved Orb edge/monitor position without cumulative drift.
- Pointer hit testing follows the visible native window; no invisible scaled rectangle may trigger hover outside it.
- Settings and the notification-area safety recovery path remain usable at every product scale.

Orb dimensions do not change with Product Scale: Plus remains 74 × 84 and Pro remains 74 × 62. Product Scale changes the Full presentation selected by the interaction contract, not the Orb geometry.

## Multi-monitor, taskbar, and work-area rules

- Placement and clamping use the destination monitor's current work area, not global desktop bounds.
- Taskbars on any edge reduce the usable work area and must never be covered by the widget after snap, expansion, collapse, DPI change, or restore.
- On a per-monitor DPI transition, retain the same product-scale state and logical hierarchy, accept the destination DPI, recalculate physical bounds, clamp to the destination work area, and preserve the saved edge anchor.
- Repeated Orb → Full → Orb transitions across monitors must not detach from the edge, move offscreen, switch monitor unexpectedly, or accumulate position error.

## Approved responsive rules

1. The 100% Full, Orb, and Settings states remain unchanged.
2. The 70% Plus Compact Full is 236 × 178; the 70% Pro Compact Full is 236 × 128.
3. Plus Compact Full retains 5 Hour and Weekly. Pro Compact Full retains Weekly.
4. Every visible quota window retains a 10-segment right-pointing arrow chain.
5. Reset information and the combined Billing / Sync / Refresh / Settings action area remain visible in Compact Full.
6. Compact Full body text is 10 DIP, supporting text is 9 DIP, and action controls are 24 DIP high.
7. Chinese and English use the same approved outer dimensions and must remain complete and unclipped.
8. There is no independent 40% Full. The 40% idle state is the fixed-size Orb and 300 ms hover opens the corresponding 70% Compact Full.
9. Settings remains 520 × 520 and never follows Product Scale.
10. Product Scale and Windows DPI remain independent systems. Existing product Topmost, 24 DIP edge threshold, work-area anchoring, and exact Orb restoration rules remain unchanged.
11. Product Scale exposes only the discrete `40%`, `70%`, and `100%` values. Selecting any value applies its layout immediately.
12. A 40%-forced Orb restores Full when the user leaves 40%; a user-selected or edge-derived Orb remains Orb.

## Scale Design Approval Pack

The static approval pack presented two candidates. Candidate A is approved as the implementation authority. Candidate B is explicitly rejected.

### Candidate A — approved

| Product scale | Plus Full | Pro Full | Plus Orb | Pro Orb | Behavior |
|---:|---:|---:|---:|---:|---|
| 100% | 278 × 216 | 278 × 156 | 74 × 84 | 74 × 62 | Accepted current Full and Orb |
| 70% | 236 × 178 | 236 × 128 | 74 × 84 | 74 × 62 | Compact Full with reflow; 10/9 DIP body/supporting floors and 24 DIP action height |
| 40% | no resting Full | no resting Full | 74 × 84 | 74 × 62 | Orb-first; 300 ms hover opens the corresponding 70% Compact Full |

### Candidate B — rejected

| Product scale | Plus Full | Pro Full | Plus Orb | Pro Orb | Behavior |
|---:|---:|---:|---:|---:|---|
| 100% | 278 × 216 | 278 × 156 | 74 × 84 | 74 × 62 | Accepted current Full and Orb |
| 70% | 236 × 178 | 236 × 128 | 74 × 84 | 74 × 62 | Same Compact Full as Candidate A |
| 40% | 210 × 156 | 210 × 112 | 74 × 84 | 74 × 62 | Independent Micro Full; all required information retained |

Candidate B carries material readability and pointer-target risk: its 22 DIP footer cannot provide the approved 24 DIP control height, and the English footer nearly exhausts the available width. It must not be implemented.

Approval artifacts:

- `design-preview/wpf-r1/scale-approval/qf-wpf-010-scale-design-approval-pack.png`
- `design-preview/wpf-r1/scale-approval/candidate-a-plus-70-zh.png`
- `design-preview/wpf-r1/scale-approval/candidate-a-plus-70-en.png`
- `design-preview/wpf-r1/scale-approval/candidate-a-pro-70-zh.png`
- `design-preview/wpf-r1/scale-approval/candidate-a-pro-70-en.png`
- `design-preview/wpf-r1/scale-approval/candidate-a-40-orb-first.png`

Only the six approved scale PNGs listed above are shipped. The main approval pack retains its historical Candidate B comparison; Candidate B is rejected. Separate B-only images, detail crops, ZIPs and render scripts are excluded.

Approved Candidate A artifact fingerprints:

| Artifact | SHA-256 |
|---|---|
| `qf-wpf-010-scale-design-approval-pack.png` | `AC1DD8018631BC8314DA7D8338DC24CE1B268854D2D85FBAE9C62F0924FF27EB` |
| `candidate-a-plus-70-zh.png` | `788A5AA25E640A38413BBDE18EE0D019767C0C6641BE7BE8B95B7F76BD401C87` |
| `candidate-a-plus-70-en.png` | `508786D3550E197241CF737840D71A164458E4793CAA2CD798EE33A4DE6108E7` |
| `candidate-a-pro-70-zh.png` | `573CED424A60147131A8EB096D8AAD9406C31E31BA386D96DC6CAA3A60C5825E` |
| `candidate-a-pro-70-en.png` | `528CCFE5C099A0AB9A2ECF099763C0D83938CB342DA40FCBE8B0BE3DDCFF59E5` |
| `candidate-a-40-orb-first.png` | `0B26008E98E51E20FEB28BAB77FE93EB6C8F81856CC9A475CDE4D28D91124703` |

## Gate result

The user explicitly approved Candidate A and rejected Candidate B. The approval artifacts, exact dimensions, typography floors, action height, 10-segment indicator rule, fixed Orb geometry, 40% Orb-first behavior, and Product Scale / Windows DPI separation are now frozen.

`QF-WPF-010 SCALE CONTRACT — PASS`

The design decision is implemented; no pending implementation gate is implied.

## Implementation acceptance

The final Revision 4 candidate implements this contract and received independent Boss acceptance at native 96 DPI. The historical acceptance is retained in local audit records, not shipped; see [Acceptance summary](ACCEPTANCE-SUMMARY.md).

`QF-WPF-010 — BOSS PASS`

Windows 125% / 120 DPI and 150% / 144 DPI remain `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE` and must not be inferred from this result.
