# Quote Float WPF R1 Implementation Specification

Status: accepted implementation authority  
Visual direction: locked  
Native runtime acceptance: accepted with documented limitations at Windows 100% / 96 DPI  
Target: C# / .NET 8 / WPF on Windows x64

## 1. Authority and precedence

For WPF R1, use this order when requirements conflict:

1. Later explicitly accepted behavior contracts override only their affected details: discrete 40% / 70% / 100% in SCALE-CONTRACT.md and the Minimal Refreshing rule in section 6.
2. The approved Civic Wayfinding PNG references for unaffected visual details.
3. This accepted implementation specification.
4. Existing source code and historical documents.

The immutable static Settings references retain a historical 40–180 slider. The later accepted discrete-scale contract supersedes that control detail; the Minimal Refreshing contract supersedes affected historical status text. These images are design references, not final native captures or executable downloads.

`PRODUCT.md`, `docs/UI-SPEC-R12.md`, the WinForms UI, and older design boards are historical inputs only for WPF R1. They contain conflicting WinForms, three-mode, three-skin, theme-selector, and 85% minimum-scale decisions and therefore are not WPF R1 visual authority.

Design is frozen. Layout or visual changes require an explicit contract change and affected native acceptance.

## 2. Approved visual baseline

All paths are repository-relative:

| State | Reference | SHA-256 |
|---|---|---|
| Chinese Full + Orb | `design-preview/wpf-r1/01-civic-full-orb-zh.png` | `7181AE9376FC272DAF97AC1B8EDD22DBDA09AAA23B6C8A4EAFA0B8D38EBEA7C3` |
| English Full + Orb | `design-preview/wpf-r1/02-civic-full-orb-en.png` | `C4AC89608CE1BDE5CAB07789362D28FBFBE5252965E234046C61B279854D851C` |
| Chinese Settings | `design-preview/wpf-r1/03-civic-settings-zh.png` | `7695121D92F5A8C387B6AF406BCCDF0B5E926BE812619B0D11AABFB724C4EEF6` |
| English Settings | `design-preview/wpf-r1/04-civic-settings-en.png` | `F0E31B07BE87B939816833909518C13EB518C3A653F5655891295844BD167140` |
| Approved scale contract | `docs/wpf-r1/SCALE-CONTRACT.md` | canonical text contract; Candidate A approved 2026-09-07 |
| Approved 70%/40% scale pack | `design-preview/wpf-r1/scale-approval/qf-wpf-010-scale-design-approval-pack.png` | `AC1DD8018631BC8314DA7D8338DC24CE1B268854D2D85FBAE9C62F0924FF27EB` |

The ten canonical PNGs listed in PUBLICATION-ALLOWLIST-V2.md are retained static acceptance references. Static images do not replace native WPF runtime evidence; ZIPs and render scripts are not shipped.

## 3. Accepted implementation baseline

WPF is the current primary implementation: `wpf/QuotaFloat.Wpf.csproj` targets `net8.0-windows`, uses WPF and builds a Windows x64 WinExe. The prior WinForms implementation is historical and is not a WPF compilation dependency.

QF-WPF-012 accepted the runtime with documented limitations. PUB-WPF-001 subsequently detached the three native source links using byte-identical WPF-owned copies and passed current-tree and independent source-view builds, 29 focused fixtures, and native 96 DPI smoke. See [Acceptance summary](ACCEPTANCE-SUMMARY.md) and [candidate manifest](CANDIDATE-MANIFEST.json) for provenance and current hashes.

Current candidate hashes:

- EXE: `77A2E2D531C56E6B5FE2A0E69A4203DD1E667ED61D88BB7FFF80A952E3C11A40`
- DLL: `CADA7E944A418F080EA43658B6C76ACCE68E05824C050AA7A66C9F0F122256E1`

## 4. Isolation and structure

WPF compiles solely from its own source tree:

- `wpf/Windows/`: main widget, Settings and product-scale layout.
- `wpf/Interaction/` and `wpf/Platform/`: transitions and monitor placement.
- `wpf/Services/`: application, refresh, preferences, presence, tray and single-instance ownership.
- `wpf/Shared/`: WPF-owned quota data, HTTP client and Codex lifecycle logic.
- `wpf/Resources/` and `wpf/Skins/`: text and the single Civic Wayfinding presentation.
- `wpf/tests/`: focused deterministic fixtures referencing the WPF project.

The skin seam remains internal; R1 exposes no skin selector or additional theme.

## 5. Locked product states

Only two widget modes exist:

- Full
- Orb

There is no Mini/capsule mode in WPF R1.

At 96 DPI and 100% application scale, logical WPF sizes are:

| Plan state | Full | Orb |
|---|---:|---:|
| Plus | `278 × 216` | `74 × 84` |
| Pro | `278 × 156` | `74 × 62` |

Settings is always `520 × 520` logical units and does not follow Full scale.

Product Scale is represented by exactly three discrete states: 40%, 70%, and 100%. Arbitrary integer percentages and values above 100% are not exposed. The behavior is governed by the Boss-approved `docs/wpf-r1/SCALE-CONTRACT.md` Candidate A contract. At 70%, Plus uses a 236 × 178 Compact Full and Pro uses a 236 × 128 Compact Full. At 40%, there is no independent Full: idle uses the fixed approved Orb (Plus 74 × 84, Pro 74 × 62) and 300 ms hover opens the corresponding 70% Compact Full. A Full forced into Orb by selecting 40% returns to the matching Full layout when 70% or 100% is selected; a user-selected or edge-derived Orb remains Orb. Settings remains 520 × 520.

Responsive scale is not a bitmap or uniform text reduction. The 70% Compact Full retains all plan-appropriate quota windows, reset information, Billing, Sync, Refresh, Settings, a 10-segment arrow chain for each visible quota window, 10 DIP body text, 9 DIP supporting text, and 24 DIP action height. Candidate B and its independent 40% Full are rejected. Product Scale selects the logical layout; Windows DPI independently maps logical units to physical pixels.

## 6. Locked data rules

- Plus displays 5-hour and Weekly when those server windows are actually returned.
- Pro reference state displays Weekly only.
- Runtime rendering remains server-driven: an absent server window produces no row. Plan name must not synthesize a missing quota window.
- Percentage is the primary readout. Each visible window has its own arrow-chain meter, remaining/reset information, and stable label.
- Reset credits show the available count and earliest valid future expiry.
- Billing opens the verified Usage & billing target and never purchases, redeems, or exchanges anything.
- Sync state remains distinct from the Refresh action: Sync reports data state; Refresh requests an update.
- Refresh presentation has one minimal state rule: with no previously accepted quota snapshot, an active request remains `Loading`; with an existing accepted quota snapshot and `IsRefreshing=true`, retain that quota content and render status `Refreshing` in English or `刷新中` in Simplified Chinese until the request completes and the final state replaces it.
- Refreshing does not clear quota data, change external window dimensions, change the Refresh or Settings actions, or add a spinner, animation, or new visual structure.
- Unknown values are not rendered as zero.
- The accepted snapshot currently requires a Weekly window; unsupported or 5-hour-only data is treated as malformed. SignedOut/SessionChanged clears the old snapshot and stops the current auto-refresh loop. Automatic real re-login recovery is not claimed.

## 7. Locked Civic visual language

- Full/Orb background: deep navy.
- Primary accent: solid amber.
- One continuous solid amber outer border; no translucent double edge, glow, gradient, texture, screenshot background, or grain.
- Full has a slim amber title band. The band must not become visually heavy.
- Progress uses a continuous right-pointing arrow chain. It must not collapse into a plain line or ordinary rectangle.
- Main numerals dominate decoration. Percent sign shares the number baseline.
- Chinese uses a native Windows CJK UI face; English uses a native Windows UI face. Do not bundle fonts or expose a font selector.
- Full bottom action row preserves Billing, Sync, Refresh, and Settings. Refresh and Settings retain visible text in the active language.
- Chinese and English are complete, separate layouts with the same hierarchy; no bilingual stacking.
- No Theme or Skin selector is visible in R1.

## 8. Window and interaction model

Use one main widget window instance. Full and Orb are states of that instance; transitions must not create accumulating windows.

State that must be distinguished:

- persisted preferred mode: Full or Orb;
- edge-collapsed Orb and its saved edge/monitor position;
- temporary hover-expanded Full;
- manual Full state;
- Settings visibility.

Required behavior:

- Always-on-top defaults to enabled.
- Releasing the widget within the accepted edge threshold converts it to Orb.
- Hovering Orb for 300 ms temporarily expands Full toward the screen interior.
- Pointer exit returns a temporary Full to the same saved Orb edge position.
- Movement between monitors uses the destination monitor work area and respects taskbars.
- Settings opens centered in the active desktop/work area, above the widget, and non-modally so the widget is not locked.
- Click-through is never restored automatically after restart; an immediately usable recovery path is mandatory.
- Codex lifecycle logic observes but never terminates Codex. Quote Float exits only after the accepted absence rule is satisfied.

WPF runtime ownership is explicit:

- QF-WPF-007 introduces the WPF notification-area recovery service used to disable click-through and exit safely. It is a safety affordance, not a third widget mode or a new skin surface.
- QF-WPF-009 introduces the WPF application coordinator that owns single-instance activation, Codex presence sampling, absence confirmation, main-window creation/closure, request cancellation, and process exit.
- The WinForms `FollowContext` and `FollowStartup` are behavioral references only. WPF must not inherit `ApplicationContext`, `System.Windows.Forms.Timer`, or WinForms `NotifyIcon` ownership.
- R1 accepts explicit direct/watch command-line modes, but no ticket may modify the desktop shortcut, registry startup entries, or launch/terminate Codex without separate authorization.

QF-WPF-009E separates deterministic implementation evidence from two real external lifecycle scenarios. QF-WPF-009 Rev 2 retains accepted coverage for 29 focused assertions, Plus/Pro mapping, loading/signed-out/stale logic, refresh/sync, single-instance behavior, deterministic Codex presence/watch behavior, privacy, and QF-WPF-005 through QF-WPF-008 regressions. R1 explicitly excludes both a same-account real Pro `Fresh -> SignedOut -> Fresh` run and an isolated test-Codex `Present -> close -> three absence confirmations -> Quote Float response -> restart/recovery` run. These scenarios remain `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`; they are not `BOSS PASS`, do not block QF-WPF-012, and must appear in final acceptance and release notes. Hyper-V, VM/Sandbox preparation, and a second Windows test environment are not part of R1.

## 9. Settings surface

The approved 520 × 520 surface contains:

- default view: Full / Orb;
- language: Simplified Chinese / English;
- Product Scale: discrete 40% / 70% / 100%, restore 100% with immediate layout application;
- Windows appearance note;
- always on top;
- edge becomes Orb;
- hover expands Full;
- mouse passthrough;
- auto refresh;
- follow Codex lifecycle;
- the low-quota-alert toggle stores a preference; notification delivery is not claimed as implemented or verified;
- refresh interval;
- Help, Reset preferences, saved state, Done.

No skin selector, theme selector, test-hover button, font selector, or unapproved setting may be added.

The Follow Codex preference is read at launch; changing it does not reconfigure the current watcher. Product Scale selections apply immediately as specified above.

All actionable WPF controls require stable `AutomationProperties.AutomationId` values; UI automation must not locate controls by visible text.

## 10. WPF-owned shared logic

`wpf/Shared/QuotaData.cs`, `QuotaClient.cs`, and `CodexLifecycle.cs` were copied byte-for-byte from the accepted native inputs in PUB-WPF-001. Their namespaces and behavior are unchanged. Normal SDK inclusion compiles these files; the WPF project contains no parent-native Compile/Link references. The WPF test project references only the WPF project.

The independent source-view build contained only non-generated WPF source/tests and no native directory. It passed Release builds and all 29 focused fixtures. Future edits to either implementation must be reviewed explicitly; these are independent copies, not automatically synchronized files.

WinForms presentation and event-loop classes are not reused as WPF views. Obsolete preference values are normalized without exposing legacy skins, themes or Mini mode.

## 11. Visual and runtime evidence gate

Every UI ticket follows:

```text
approved reference -> implement -> build -> launch real WPF candidate
-> put candidate in the required state -> capture native desktop screenshot
-> compare by region -> targeted fix -> recapture -> regression check
-> PASS or FAIL
```

Build success and offscreen/browser rendering are not native acceptance.

Each runtime evidence set records:

- executable path and SHA-256;
- process ID and launch command;
- source revision/fingerprint;
- Windows version, monitor, resolution, work area, DPI/scaling;
- capture time and exact state/steps;
- approved reference path;
- raw screenshot path;
- region-by-region comparison and known differences.

Dynamic time/quota values may be masked in comparison. Geometry, spacing, typography, meter shape, control position, and clipping may not be masked.

Historical evidence remains in local `output/wpf-r1/` audit records and is not shipped. The public acceptance summary is self-contained.

## 12. Review and publication boundary

Changes require a bounded scope, protected-source checks, relevant validation and independent acceptance. Compilation, deterministic tests and real desktop interaction are distinct evidence levels. Publish only the exact paths in [Publication allowlist](PUBLICATION-ALLOWLIST-V2.md). Historical ticket files and local audit logs are not shipped.

Acceptance does not authorize staging, commits, pushes, merges, tags, installation or release uploads. Generated binaries require a separately authorized release build and candidate review.

## 13. Completion definition

WPF R1 is ACCEPTED WITH DOCUMENTED LIMITATIONS after QF-WPF-012 BOSS PASS. QF-WPF-009E and native Windows 125%/150% DPI acceptance remain approved R1 scope exclusions and remain `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE` in the final acceptance record and release notes. Static Civic visual acceptance remains PASS but is not used by itself to claim runtime, DPI, lifecycle, interaction, or performance completion. The historical final recheck is a local audit record, not shipped. Current source-detached candidate identity and inherited acceptance limits are recorded in [Acceptance summary](ACCEPTANCE-SUMMARY.md).

## Regression evidence reuse rule

Apply this rule to acceptance rechecks, publication preparation, and later maintenance:

- Relevant code changed -> rerun the relevant heavy gate.
- Relevant code unchanged -> reuse accepted evidence plus lightweight smoke.

Evidence reuse requires candidate/source hash and changed-file analysis showing that the tested behavior, dependencies, packaging, and runtime configuration remain applicable. A new build alone neither invalidates every prior result nor proves continued validity. Do not repeat an already accepted lifecycle or stress suite without a changed input, affected behavior, or new defect reason.
