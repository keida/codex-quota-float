# Third-party notices / 第三方说明

## quota-float reference

Feature behavior and quota-field exploration referenced:

- Project: [change-42-yhmm/quota-float](https://github.com/change-42-yhmm/quota-float)
- Revision: `3e3848f4161b912166dcde98fcb94efb3630ddde`
- Reference: [src-tauri/src/codex.rs](https://github.com/change-42-yhmm/quota-float/blob/3e3848f4161b912166dcde98fcb94efb3630ddde/src-tauri/src/codex.rs)
- License: [MIT at the referenced revision](https://github.com/change-42-yhmm/quota-float/blob/3e3848f4161b912166dcde98fcb94efb3630ddde/LICENSE)

This repository contains a C# / WinForms implementation, not the reference project's Rust/Tauri distribution. The upstream copyright and permission notice is retained below for the referenced material. This acknowledgment does not imply endorsement or feature parity.

本项目参考了上述版本的行为与额度字段，并采用 C# / WinForms 实现；未分发其 Rust/Tauri 发行包。下面保留参考材料的上游版权与许可声明；致谢不代表对方背书或功能完全等价。

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

The current project files declare no third-party NuGet package references. The app requires .NET 8 Desktop Runtime and Windows system APIs. Framework-dependent publication does not bundle the Desktop Runtime. Fonts and tray icons are obtained through Windows/.NET APIs; no font files, third-party icon packs, or concept images are included in this source publication.

当前项目文件没有第三方 NuGet 包引用。应用依赖 .NET 8 Desktop Runtime 与 Windows 系统 API；依赖运行时的发布方式不打包 Desktop Runtime。字体与托盘图标通过 Windows/.NET API 获取，本次源码公开不包含字体文件、第三方图标包或概念图。

If future releases bundle a runtime, package, font, image, or other asset, review and include that component's redistribution notices before publishing it. OpenAI, Codex, Microsoft, and Windows names identify compatibility targets; no affiliation or endorsement is implied.
