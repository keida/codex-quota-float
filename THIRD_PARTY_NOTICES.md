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
