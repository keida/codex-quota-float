# Third-party notices / 第三方说明

## quota-float reference

Feature behavior and quota-field exploration referenced:

- Project: [change-42-yhmm/quota-float](https://github.com/change-42-yhmm/quota-float)
- Revision: `3e3848f4161b912166dcde98fcb94efb3630ddde`
- Reference: [src-tauri/src/codex.rs](https://github.com/change-42-yhmm/quota-float/blob/3e3848f4161b912166dcde98fcb94efb3630ddde/src-tauri/src/codex.rs)
- License: [MIT at the referenced revision](https://github.com/change-42-yhmm/quota-float/blob/3e3848f4161b912166dcde98fcb94efb3630ddde/LICENSE)

The current primary implementation is C# / .NET 8 / WPF. The legacy `native/` tree is a historical C# / WinForms implementation. The upstream Rust/Tauri distribution is not redistributed. The upstream copyright and permission notice is retained below for the referenced material; this acknowledgment does not imply endorsement or feature parity.

当前主实现为 C# / .NET 8 / WPF；`native/` 目录是历史 C# / WinForms 实现。本项目不分发上游 Rust/Tauri 发行包。下面保留参考材料的上游版权与许可声明；致谢不代表对方背书或功能完全等价。

```text
MIT License

Copyright (c) 2026 Quota Float contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## Runtime and system resources / 运行时与系统资源

The current WPF project files declare no third-party NuGet package references. The framework-dependent binary requires the .NET 8 Desktop Runtime and Windows/.NET APIs. Fonts and tray assets are obtained through Windows/.NET APIs. The ten historical static design PNGs previously listed in `docs/wpf-r1/PUBLICATION-ALLOWLIST-V2.md` were removed from the repository and are not current product screenshots or release assets. This notice is not legal advice or a comprehensive legal audit.

当前 WPF 项目文件没有第三方 NuGet 包引用。依赖框架的二进制需要 .NET 8 Desktop Runtime 与 Windows/.NET API。字体与托盘资源通过 Windows/.NET API 获取。此前列于 `docs/wpf-r1/PUBLICATION-ALLOWLIST-V2.md` 的十张历史静态设计 PNG 已从仓库移除，不是当前产品截图或发布资产。本说明不是法律意见，也不是全面法律审计。

If future releases bundle a runtime, package, font, image, or other asset, review and include that component's redistribution notices before publishing it. OpenAI, Codex, Microsoft, and Windows names identify compatibility targets; no affiliation or endorsement is implied.

## R2 beta direct dependencies / R2 Beta 直接依赖

The R2 source directly declares the following runtime/build dependencies. License expressions below are copied from the locked package metadata in the source package manifests; they are not a complete transitive dependency or redistribution audit.

| Component | Locked version | Manifest license expression |
| --- | --- | --- |
| Tauri / `tauri-build` | 2.12.4 / 2.7.3 | Apache-2.0 OR MIT |
| `base64` | 0.23.1 | MIT OR Apache-2.0 |
| `chrono` | 0.4.45 | MIT OR Apache-2.0 |
| `reqwest` | 0.13.5 | MIT OR Apache-2.0 |
| `serde` / `serde_json` | 1.0.229 / 1.0.154 | MIT OR Apache-2.0 |
| `sha2` | 0.10.9 | MIT OR Apache-2.0 |
| React / React DOM | 19.3.1 / 19.3.1 | MIT |
| `@tauri-apps/api` / CLI | 2.12.4 / 2.12.3 | Apache-2.0 OR MIT |
| TypeScript / Vite / Vitest | 7.0.2 / 8.3.4 / 5.0.4 | Apache-2.0 / MIT / MIT |
| `@testing-library/jest-dom` / React / user-event | 7.0.1 / 16.3.3 / 14.6.8 | MIT |
| `@types/node` / React / React DOM | 26.6.5 / 19.3.0 / 19.3.0 | MIT |
| `@vitejs/plugin-react` / jsdom | 6.1.2 / 30.1.2 | MIT |

The UI test libraries and Vite React plugin are development dependencies, not runtime components; their lockfile versions and manifest license fields remain in `package-lock.json` and package metadata. The source build uses the system WebView2 runtime. `bundle.active` remains false, so this source does not redistribute a WebView2 runtime, installer, or updater. The icon artwork is original project artwork and does not embed third-party font or image assets.

R2 源码直接声明的运行时/构建依赖见上表。许可表达式来自锁定包的 manifest 元数据；这不是对传递依赖或再分发义务的完整审计。UI 测试库和 Vite React 插件仅为开发依赖，不属于运行时组件；其锁定版本和许可字段保留在 `package-lock.json` 与包元数据中。源码构建使用系统 WebView2 runtime。`bundle.active` 仍为 false，因此源码不再分发 WebView2 runtime、安装程序或更新器。图标为项目原创图稿，不嵌入第三方字体或图像资源。
