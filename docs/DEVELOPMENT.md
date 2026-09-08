# WPF R1 development / 开发说明

The primary project is Windows x64, C# / .NET 8 WPF. Use the .NET 8 SDK on Windows; the framework-dependent executable requires the .NET 8 Desktop Runtime. WPF builds without `native/`.

## Build and deterministic tests

Run from the repository root. Before running the focused harness, exit any running Quote Float instance; it shares the production single-instance mutex/event and must become the primary instance. Do not close Codex.

```powershell
dotnet build wpf/QuotaFloat.Wpf.csproj -c Release --nologo
dotnet build wpf/tests/QuotaFloat.Wpf.Tests.csproj -c Release --nologo
dotnet run --project wpf/tests/QuotaFloat.Wpf.Tests.csproj -c Release --no-build
```

The accepted focused output reports `fixtures=29; activeRequests=1; refreshCalls=1; backoffCalls=3; privacy=normalized-values-only`. These account-free fixtures cover parsing/state mapping, refresh orchestration, session boundaries, single-instance and deterministic absence seams. They are not 29 real-user scenarios and do not prove live login/logout or Codex close/restart behavior.

## Run

```powershell
dotnet run --project wpf/QuotaFloat.Wpf.csproj -c Release --no-build -- --direct
dotnet run --project wpf/QuotaFloat.Wpf.csproj -c Release --no-build -- --demo-state plus --demo-language en --demo-scale 100 --demo-exit-ms 10000
```

Run one command at a time. The first uses local authentication and HTTPS quota requests; the second uses synthetic data and auto-exits. Demo states include `plus`, `pro`, `orb-plus`, `orb-pro`; languages `zh`/`en`; scales `40`/`70`/`100`.

`--watch` observes Codex and exits after three confirmed absences. It does not launch Codex. Changes to the follow preference take effect at the next launch. With no mode switch, the saved follow preference applies (initially true); `--direct` wins if both mode switches occur. Do not alter real authentication or close user Codex processes to run deterministic tests.

## Project map

| Path | Responsibility |
|---|---|
| `wpf/Windows/` | Full/Orb, Settings, product-scale layout |
| `wpf/Interaction/`, `wpf/Platform/` | Transitions, monitor/work-area placement |
| `wpf/Services/` | Refresh, presence, single instance, preferences and tray ownership |
| `wpf/Shared/` | WPF-owned quota data/client and Codex lifecycle copies |
| `wpf/Resources/`, `wpf/Skins/` | Text and internal Civic presentation |
| `wpf/tests/` | Focused fixtures; project reference to WPF |

PUB-WPF-001 copied the three Shared files without logic or namespace changes and removed native Compile/Link references. Its independent source view contained 30 non-generated WPF files and no native directory; builds and 29 fixtures passed. Current candidate provenance is in [the manifest](wpf-r1/CANDIDATE-MANIFEST.json). Cross-root byte reproducibility is not claimed.

## Acceptance and changes

- Relevant code changed -> rerun the relevant heavy gate.
- Relevant code unchanged -> reuse accepted evidence plus lightweight smoke.

Reuse requires source/candidate hashes and changed-file analysis that covers dependencies, packaging and runtime configuration.

UI tests use stable AutomationIds and real candidate identity, DPI, geometry and exit evidence. Use synthetic data; never submit auth files, tokens, response bodies or private screenshots. Distinguish static checks, executed tests and native interaction.

1. Same real Pro account `Fresh -> SignedOut -> Fresh` — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
2. Isolated Codex `Present -> close -> three absence confirmations -> Quote Float response -> restart/recovery` — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.
3. Native Windows 125% / 120 DPI and 150% / 144 DPI runtime acceptance — `NOT VERIFIED / OUT OF R1 ACCEPTANCE SCOPE`.

Native verified baseline: Windows 100% / 96 DPI.

运行 focused harness 前请先退出已经运行的 Quote Float；它与生产程序共享单实例 mutex/event，测试必须取得 primary instance。不要关闭 Codex。本轮发布文档清理不运行构建、测试或界面；以上命令是开发说明，历史执行结果由验收摘要注明来源。旧 WinForms 修改保持独立，不纳入 WPF 源码提交。

See [acceptance](wpf-r1/ACCEPTANCE-SUMMARY.md) and [publication selection](wpf-r1/PUBLICATION-ALLOWLIST-V2.md). Binary release creation, versioning, signing and installation remain separate publication decisions.
