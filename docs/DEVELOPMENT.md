# Development / 开发说明

## Requirements / 环境

Windows x64 and the .NET 8 SDK. The source-only tests use synthetic quota data and do not require a Codex account. Running the real widget requires a signed-in Codex Desktop session; see the root README for privacy boundaries and known limitations.

需要 Windows x64 与 .NET 8 SDK。测试使用构造的额度数据，不需要 Codex 账号；运行真实浮窗需要已登录的 Codex Desktop。

## Project map / 项目结构

| Area | Responsibility |
| --- | --- |
| `native/QuotaClient.cs`, `QuotaData.cs` | Read-only quota requests and defensive parsing |
| `native/CodexLifecycle.cs`, `FollowContext.cs`, `FollowStartup.cs` | Official MSIX discovery, window presence, opt-in login following |
| `native/QuotaForm.cs`, `SettingsForm.cs`, `NativeWindowTools.cs` | Native rendering, settings, resizing and edge behavior |
| `native/Preferences.cs`, `RuntimeDiagnostics.cs` | Local preferences and bounded lifecycle breadcrumbs |
| `native/tests/` | Parser, preferences, native-window and follow regression checks |
| `native/tests-lifecycle/` | Isolated real-process/window lifecycle checks |

See [lifecycle notes](../native/LIFECYCLE-NOTES.md) for the presence contract. The host project deliberately has its own directory and build output; do not combine it with the lifecycle test project's `obj` directory.

## Build and test / 构建与测试

Run from the repository root. Start with the account-free checks:

```powershell
dotnet run --project native/tests/QuotaFloat.Tests.csproj -c Release
dotnet publish native/QuotaFloat.csproj -c Release -r win-x64 --self-contained false -o release
```

Native-window tests require an interactive Windows desktop. They open controlled test windows, not the user's Codex, and do not fetch quota or install login startup:

```powershell
dotnet run --project native/tests/QuotaFloat.Tests.csproj -c Release -- --resize-snap --windows
dotnet build native/tests-lifecycle/host/QuotaFloat.Lifecycle.Host.csproj -c Release
dotnet run --project native/tests-lifecycle/QuotaFloat.Lifecycle.Tests.csproj -c Release
dotnet run --project native/tests/QuotaFloat.Tests.csproj -c Release -- --follow --host-path native/tests-lifecycle/host/bin/Release/net8.0-windows/QuotaFloat.Lifecycle.Host.exe
```

窗口测试需要可交互的 Windows 桌面，会打开受控测试窗口，不关闭真实 Codex、不访问额度或安装启动项。自动测试通过不代表多屏、全部 DPI 或真实登录/退出流程已经验收。

The published output is a framework-dependent EXE. Do not overwrite a running copy; quit Quota Float first or publish to another output directory. No signing, binary Release upload, startup registration, or automatic update is performed by these build commands.

## Contribution and evidence / 贡献与证据

- Keep changes focused and describe the behavior being fixed in your issue or pull request.
- Use synthetic data in fixtures and screenshots. Never submit login files, tokens, raw API responses, personal diagnostics, or local account screenshots.
- Separate static review, executed automated tests, and actual desktop interaction in your report. Do not call an untested screen layout or lifecycle scenario verified.
- Keep request destinations, TLS validation, read-only behavior, account-switch boundaries, and opt-in startup explicit when changing related code.
- Do not advertise prototype-only features or zero resource usage. Contributions should preserve source attribution and compatible license notices.

提交 Issue/PR 时说明复现步骤和验证范围；仅使用演示数据，不上传账号或本机诊断。修改应保持读取额度的权限边界、来源署名与许可证声明。公开反馈入口见仓库 Issues；敏感细节不要直接贴到公开 Issue。
