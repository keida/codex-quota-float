# R2 Changelog

## 2.0.0-beta.1 — 2026-10-17 — prepared, not published

This is the first versioned public-source preview for the separate Tauri R2 line. It is not a GitHub Release, stable replacement for WPF R1, installer, signed binary, or updater.

### Added

- Rust/Tauri 2 desktop shell with a React + TypeScript interface.
- Compact quota widget with hover expansion, Settings, tray Open/Refresh/Exit, window drag, and edge-aware placement logic.
- Live Codex quota/reset-credit retrieval with quota-specific refresh feedback, single-flight refresh behavior, interval polling, and retry/backoff handling.
- English and Simplified Chinese UI/tray text; System, Light, and Dark themes.
- Local token-count activity chart from bounded Codex session JSONL scanning, with partial coverage presented as a lower bound rather than fabricated completeness.
- A high-resolution quota-ring icon source, multi-size Windows ICO, and deterministic preview/generation script.

### Source-only plan-label update — 2026-10-17 (version unchanged)

- Clarified the current-plan badge: `prolite` → `PRO LITE`, `plus` → `PLUS`, `pro` → `PRO (MORE)`, and `promax` → `PRO (MAX)`, case-insensitively. Unknown values remain readable; null or blank values use the localized Unknown fallback. The raw backend identifier is retained, and the badge appears only in the expanded detail header.
- The pinned [Codex auth source](https://github.com/openai/codex/blob/ce0323f2f2d3b25e82806cd555237da3db4e1897/codex-rs/protocol/src/auth.rs#L130-L145) displays `KnownPlan::ProLite` as the generic `Pro`. R2's `PRO LITE` label is a deliberate UI distinction for raw `prolite`, separate from raw `pro` → `PRO (MORE)`; it is not an upstream display spelling or billing claim.
- R2 remains independent of the Codex Desktop process: it does not monitor Codex or exit automatically when Codex closes. Its window-close action hides R2 to the tray; tray Exit quits. The quota path reads local authentication data and makes direct HTTP requests, with authentication/request problems represented as quota statuses. These are source-level statements; every lifecycle and failure scenario has not received native runtime testing.
- Built a new local unpackaged x64 custom-protocol candidate (20,928,512 bytes; SHA-256 `5BF6AAFB87791556F543863FF7D8691CA363CFB2727A4351DB00A9C2FF36099A`). The user reported the candidate worked without issue. It is not a GitHub Release or public binary; `2.0.0-beta.1` remains unchanged.
- The frontend suite passed 53 tests, Rust unit tests passed 20, and the frontend and custom-protocol builds succeeded. The Rust build emitted two warnings; see the current [acceptance record](ACCEPTANCE.md) for scope and limitations.

### Compatibility and claims

- R1 v1.1.3 is the project's documented published R1 release. R2 is a separate beta source line.
- R2 does not claim full Plus/Pro parity, every account state, or broad native-DPI acceptance.
- R2 is not ChatGPT ordinary-chat/API usage monitoring, an app-server client, real-time approval/session control, prediction, reminder, or Codex-follow feature.
- The local activity chart counts token usage, not quota consumed. The scanner reads bytes from conversation-bearing session records; no “never reads conversation data” promise is made.
- Windows WebView2 is required. This source build is unsigned and unpackaged, with no installer or auto-updater.

### Verification status

The original beta.1 candidate checks and smoke are preserved in `ACCEPTANCE.md` under their original hash, separately from the updated plan-label candidate. Prior Rev29 interaction confirmation is historical evidence for those observed flows, not a blanket acceptance of this candidate. Native WebView focus ordering, broad DPI, provider/account-switch behavior, clean-machine portability, shell icon rendering, and complete Plus/Pro parity remain outside verified coverage. See [Update — 2026-10-17](UPDATE-2026-10-17.md) for the bilingual current-source summary.
