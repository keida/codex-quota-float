# R2 Development

## End-user requirements

The one-file candidate is unpackaged, not self-contained. Running it requires Windows x64, the Microsoft Edge WebView2 Runtime, a usable local Codex authentication file, and network access for quota requests. End users do not need to install .NET, Node.js, Rust, or Visual Studio Build Tools. Microsoft includes WebView2 with Windows 11 and most Windows 10 devices have it, but it can be absent on some machines; consult [Microsoft's distribution guidance](https://learn.microsoft.com/microsoft-edge/webview2/concepts/distribution). This beta's clean-machine portability is not verified. R2 does not bundle or install WebView2.

## Build prerequisites

- Windows x64.
- Node.js and npm versions compatible with the locked Vite 8 toolchain.
- Rust stable with the MSVC x64 target, Visual Studio 2022 C++ Build Tools, and a Windows SDK.
- Microsoft Edge WebView2 Runtime for running the desktop executable.
- Python 3.10+ with Pillow 12.3.0 only when regenerating the icon assets. Pillow is a maintainer tool; it is not an application dependency.

The checked-in `package-lock.json` and `src-tauri/Cargo.lock` pin the JavaScript and Rust dependency graphs. Do not regenerate either lockfile as part of an ordinary source build.

## Install and validate

From `r2-tauri/`, install the exact npm lockfile once on a network-enabled development machine:

```powershell
npm ci
npm test -- --run
npm run build
cargo test --manifest-path src-tauri/Cargo.toml --locked --jobs 2
cargo build --manifest-path src-tauri/Cargo.toml --locked --jobs 2 --features custom-protocol
```

The default Cargo commands use the network as needed to obtain locked crates. For a fully cached machine, the equivalent offline commands are:

```powershell
cargo test --manifest-path src-tauri/Cargo.toml --locked --offline --jobs 2
cargo build --manifest-path src-tauri/Cargo.toml --locked --offline --jobs 2 --features custom-protocol
```

Offline mode requires the local registry cache to contain every locked crate. The custom-protocol build expects `dist/` to have been produced by `npm run build`. No installer is generated because `bundle.active` is false.

## Current local source candidate

The `2.0.0-beta.1` source received a plan-label update on 2026-10-17 without changing its version or dependency graph. The resulting local unpackaged x64 candidate is 20,928,512 bytes, SHA-256 `5BF6AAFB87791556F543863FF7D8691CA363CFB2727A4351DB00A9C2FF36099A`. This hash identifies a local candidate only; it is not a GitHub Release asset or public download. The frontend suite (53 tests), Rust suite (20 tests), frontend build, and locked offline custom-protocol build passed for this source. The Rust build emitted two compiler warnings; the [acceptance record](ACCEPTANCE.md) separates this candidate from the earlier beta.1 executable.

Recognized current plan identifiers are displayed as `PRO LITE`, `PLUS`, `PRO (MORE)`, and `PRO (MAX)` for `prolite`, `plus`, `pro`, and `promax`, respectively. The raw identifier remains in the model, and unknown values are not coerced to a known plan. The pinned [Codex auth source](https://github.com/openai/codex/blob/ce0323f2f2d3b25e82806cd555237da3db4e1897/codex-rs/protocol/src/auth.rs#L130-L145) displays `KnownPlan::ProLite` as generic `Pro`; R2 deliberately uses `PRO LITE` to distinguish raw `prolite` from raw `pro` → `PRO (MORE)`. These are R2 UI labels, not upstream spelling or billing terms.

This source branch does not prescribe credential setup or account mutation. Do not add tokens, `auth.json`, raw API responses, session records, local profiles, screenshots, or machine-specific diagnostics to the repository.

## Icon source and generation

`assets/icon/codex-float-r2.svg` is the editable vector artwork. `src-tauri/icons/icon.ico` is the Windows application/window/tray icon and the source file to use if a shortcut is created after acceptance. The 512px and 1024px PNGs are previews, not runtime assets.

Regenerate and verify all assets with:

```powershell
python scripts/generate_icon.py
```

The script writes 16, 20, 24, 32, 48, 64, 128, and 256 pixel ICO entries, 512px and 1024px RGBA previews, and the SVG. It verifies each ICO size, transparent top-left corner, opaque center, preview dimensions/alpha, and SVG viewBox. It requires the already available Pillow 12.3.0 maintainer runtime for repeatable raster output; Pillow is not an app dependency. The Tauri config references `icons/icon.ico`; the tray uses Tauri's default window icon. File-level alpha and size checks do not prove tray rendering or small-size legibility on every Windows shell/theme.

## Version fields

`2.0.0-beta.1` is aligned across `package.json`, `package-lock.json`, `src-tauri/Cargo.toml`, `src-tauri/Cargo.lock`, and `src-tauri/tauri.conf.json`. Package/crate identifiers and the Tauri application identifier are intentionally retained to avoid unrelated migration behavior. The Tauri product/window name is `Codex Float R2`.
