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
cargo test --manifest-path src-tauri/Cargo.toml --locked
cargo build --manifest-path src-tauri/Cargo.toml --locked --features custom-protocol
```

For a machine whose Cargo registry cache already contains every locked crate, add `--offline` to Cargo commands. The custom-protocol build expects `dist/` to have been produced by `npm run build`. No installer is generated because `bundle.active` is false.

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
