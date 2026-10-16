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

### Compatibility and claims

- R1 v1.1.3 is the project's documented published R1 release. R2 is a separate beta source line.
- R2 does not claim full Plus/Pro parity, every account state, or broad native-DPI acceptance.
- R2 is not ChatGPT ordinary-chat/API usage monitoring, an app-server client, real-time approval/session control, prediction, reminder, or Codex-follow feature.
- The local activity chart counts token usage, not quota consumed. The scanner reads bytes from conversation-bearing session records; no “never reads conversation data” promise is made.
- Windows WebView2 is required. This source build is unsigned and unpackaged, with no installer or auto-updater.

### Verification status

Frontend tests, Rust unit tests, frontend build, and locked offline x64 custom-protocol build are recorded in `ACCEPTANCE.md`. A prior Rev29 candidate with byte-identical frontend behavior received human-assisted confirmation for tray Open → compact, hover expand → leave collapse, and Settings remaining open on pointer leave. That acceptance does not validate the new icon/version packaging candidate. Native WebView focus ordering, broad DPI, provider/account-switch behavior, and complete Plus/Pro parity remain outside verified coverage.
