# R2 Acceptance Record — 2.0.0-beta.1 source candidates

## Scope and outcome

**The updated plan-label candidate passes frontend, Rust, and build checks and was reported as working in human testing; broad native acceptance remains incomplete.** R2 remains a source preview, not a GitHub Release. The original beta.1 candidate and the updated local candidate have different hashes and are documented separately below.

## Current plan-label candidate — 2026-10-17

The version remains `2.0.0-beta.1`; this is a source-only update, not a formal release or public download. The new unpackaged x64 Windows GUI executable is 20,928,512 bytes with SHA-256 `5BF6AAFB87791556F543863FF7D8691CA363CFB2727A4351DB00A9C2FF36099A`. Its copied candidate hash and size match the build output. The prior first-candidate executable hash `22F380596C81C32989A1C54599B4EA2071F54ED803DAB50399C7916509BCCC94` identifies a different artifact and must not be used to verify this one.

The frontend-only plan-label change displays `prolite` as `PRO LITE`, `plus` as `PLUS`, `pro` as `PRO (MORE)`, and `promax` as `PRO (MAX)`, case-insensitively. Unknown identifiers remain readable; null or blank values use the localized Unknown fallback. The raw model value is unchanged, and the 14px bold badge is limited to the expanded detail header, with Plus in mint, the Pro family in violet, and unknown values neutral. The pinned [Codex auth source](https://github.com/openai/codex/blob/ce0323f2f2d3b25e82806cd555237da3db4e1897/codex-rs/protocol/src/auth.rs#L130-L145) displays `KnownPlan::ProLite` as generic `Pro`; R2 deliberately uses `PRO LITE` for raw `prolite` to distinguish it from raw `pro` → `PRO (MORE)`. These are R2 UI labels and do not establish billing, price, or quota-window rules.

The source has no Codex-process monitoring or auto-exit dependency. Closing Codex does not automatically close R2; closing R2's window hides it to the tray, and tray Exit quits the app. The quota reader uses local authentication data and direct HTTP requests, so a usable local auth file and network are sufficient for that request path without Codex Desktop running. Missing/invalid authentication and request failures are represented as quota availability states. These are source-level behaviors; this candidate's human report did not include a detailed no-Codex or failure-state trace.

| Current check | Result | Boundary |
| --- | --- | --- |
| Frontend suite | PASS, 53/53 | Component and interaction tests; not native WebView behavior |
| Frontend build | PASS | TypeScript + Vite output from the updated source |
| Rust unit tests | PASS, 20/20 | No account/provider calls |
| Locked offline x64 MSVC custom-protocol build | PASS | Unpackaged executable; two existing compiler warnings; unsigned |
| Candidate identity | PASS | x64 PE, Windows GUI subsystem 2; copied file hash and size match the build output |
| Human test report | LIMITED PASS | User reported the candidate worked without issue; no detailed public interaction trace or capture was supplied |

This human report is not evidence of broad DPI, provider/account-switch, security, clean-machine, shell-icon, or every interaction path. The current candidate was not used to claim those results.

## Historical first-candidate checks — SHA-256 `22F380596C81C32989A1C54599B4EA2071F54ED803DAB50399C7916509BCCC94`

| Check | Result | Boundary |
| --- | --- | --- |
| `npm test -- --run` | PASS, 51/51 | JSDOM interaction/component tests; not native WebView events |
| `npm run build` | PASS | TypeScript + Vite output |
| `cargo test --manifest-path src-tauri/Cargo.toml --locked --offline --jobs 2` | PASS, 20/20 | Rust unit tests; no account/provider calls |
| `cargo build --manifest-path src-tauri/Cargo.toml --locked --offline --jobs 2 --features custom-protocol` | PASS, x64 MSVC | Unpackaged executable build; unsigned |
| Icon generator | PASS | 8 ICO entries (16–256px), transparent corners, opaque center, endpoint geometry checked; 512/1024 RGBA previews and SVG viewBox checked; 1024px and pixel-enlarged 16/20/24/32px outputs visually inspected |

The built x64 PE candidate was copied without modifying any existing candidate to `candidates/r2-beta1/Codex-Float-R2-2.0.0-beta.1.exe` (20,928,512 bytes; SHA-256 `22F380596C81C32989A1C54599B4EA2071F54ED803DAB50399C7916509BCCC94`). The copied file hash matches the build output. The previously accepted Rev29 final remains unchanged at SHA-256 `C5CBC9040DDC9BE05F58097105281972E0F65BCA4555FF523C8C88F882FE4F17`.

The same exact frontend source hashes as the human-accepted Rev29 build were retained for `src/App.tsx` (`3B87296C9900477F3261A1EE7E0FA9D8C3CFFD3E929C11DBDAD2ECAFEBA80DA1`) and `src/App.test.tsx` (`F96FE6D95DDCEE26F3AB5C2FBD32B9EA21701CD290135443073A05852E6EBF81`). Metadata, icon, documentation, and packaging wiring changed; frontend behavior did not.

## Existing interaction acceptance

The canonical Rev29 final candidate, SHA-256 `C5CBC9040DDC9BE05F58097105281972E0F65BCA4555FF523C8C88F882FE4F17`, received human-assisted confirmation for the three requested flows: tray Open restores compact; hover expands and leaving collapses; Settings stays open on pointer leave. This is not automated pointer instrumentation. It is evidence for the behavior-preserving frontend carried into this source tree, not native acceptance of this candidate's new icon, Tauri metadata, or packaging.

## Candidate-specific native Windows smoke

On 2026-10-17 (Pacific/Auckland), the exact candidate hash `22F380596C81C32989A1C54599B4EA2071F54ED803DAB50399C7916509BCCC94` was launched using a new isolated WebView profile. Host evidence bound the app process and child WebView to the candidate. The smoke observations were:

- Startup showed the compact 96-DIP widget.
- Clicking the widget opened a 340×480 detail surface with live quota, activity, and reset-credit data.
- Settings opened at 340×360; pressing Escape returned to the compact widget.

This was a tool-driven startup/basic-GUI smoke test only. It does **not** establish the hover-expand/leave-collapse sequence, tray-menu actions, embedded or shell-rendered icon appearance, broad DPI behavior, clean-machine portability, or full account/provider coverage. Raw process/window identifiers, account captures, and the private WebView profile path are intentionally omitted.

## Not verified

- Confirming the embedded executable/window/tray icon and its appearance in the Windows shell.
- WebView element-focus restoration and event ordering for native `show`/`set_focus` versus the tray action.
- Icon legibility at 16/20/24px in actual Windows tray/shortcut/shell rendering; only pixel dimensions and alpha were verified.
- Broad Windows DPI scaling, all monitor/work-area combinations, provider/account-switch transitions, and complete Plus/Pro parity.
- Any hosted release, signature, installer, updater, or user-facing shortcut installation.
- Clean-machine portability and missing-WebView2 recovery behavior.

The limited smoke run does not replace the separately scoped acceptance of hover and tray interactions, icon rendering, DPI, and clean-machine behavior. Do not infer those PASS results from this smoke, the previous Rev29 executable, component tests, or icon file inspection.
