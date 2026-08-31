# Codex lifecycle notes

`CodexLifecycle` observes the installed Codex desktop MSIX without owning it.

## Presence contract

- `Presence` is a read-only `CodexPresence` snapshot. `IsPresent` means at least one visible top-level window belongs to a process whose executable exactly matches an official `OpenAI.Codex` MSIX manifest; `ProcessIds` contains only those visible-window owner PIDs and `VisibleWindowCount` counts their visible top-level windows. A minimized window remains visible to this test and therefore remains present.
- `RefreshPresence()` performs one bounded, read-only enumeration. It does not create a permanent watcher, startup entry, service, WMI subscription, or other external registration. A supervisor may call it on its own lifetime-scoped timer. `PresenceChanged` is raised only when the snapshot changes and carries both the previous and current snapshots.
- Presence discovery re-reads all official installed package roots (cached for up to 30 seconds) and accepts every matching manifest executable path, so an update/replacement package is not mistaken for the CLI or a user-provided `ChatGPT.exe`.

## Production discovery and launch

- Package roots come from the local AppX repository registry (with a read-only `WindowsApps` directory fallback). The manifest is then validated as `OpenAI.Codex` under an official `WindowsApps` root.
- The manifest's `Application` entry for `app/ChatGPT.exe` supplies the executable path and app ID. The AUMID is derived from the manifest identity and the package-family publisher suffix; package versions are never hardcoded.
- An existing process is accepted only when its executable path exactly matches that manifest path. `ChatGPT.exe` children are excluded when a matching parent process exists, leaving the desktop root. The CLI under `%LOCALAPPDATA%\OpenAI\Codex\bin` and the `app/Codex.exe` launcher are not production matches.
- If no root exists and launching is requested, activation uses `explorer.exe shell:AppsFolder\\<AUMID>`, followed by bounded discovery polling for at most 20 seconds at 250 ms intervals.

## Exit and ownership boundary

`Process.Exited` with `EnableRaisingEvents` is the immediate process signal. The lifecycle object observes every matching process and waits briefly before confirming an exit, allowing an official root process to be replaced without raising `DesktopExited` for the old root. `DesktopExited` is raised only after the attached process and every matching replacement process are gone; it never kills, closes, or restarts a process. The event is raised from a process-exit callback thread, so a WinForms consumer must marshal UI work to its UI thread. Dispose only detaches observation and releases local `Process` handles.

After attach returns, the UI should re-read `DesktopProcessId` when its main window is shown. A `null` value means the root exited during the attach-to-subscribe handoff; the UI should close instead of presenting a live quota window.

If the user closes the visible Codex window but the desktop process remains running in the background, `Presence` becomes empty and `PresenceChanged` lets the supervisor decide whether to close or keep the widget. Window matching is based on `EnumWindows`, visibility, owner PID, and the manifest executable path—not titles, command lines, or child-process disappearance. Minimized windows do not become absent.

`AttachTestProcessAsync` is a separate test-only seam. Its caller must create a temporary process in explicit `--test-parent` mode and pass its PID; it is never used for production discovery and does not inspect or print command lines.

## Limits

- Activation depends on the user-installed MSIX being registered and discoverable through AppX registry state or `WindowsApps` access.
- A cancellation token cancels an in-flight attach/launch operation. Dispose also cancels lifecycle work; cancellation is surfaced as `OperationCanceledException`.
- This component does not create a permanent monitor, startup registration, credentials, or external shortcut. The application-level `FollowContext` supplies the presence timer, and `FollowStartup` separately implements opt-in current-user login startup.

## Controlled regression signal

`native/tests-lifecycle/QuotaFloat.Lifecycle.Tests.csproj` launches independent real WinForms hosts from `native/tests-lifecycle/host/QuotaFloat.Lifecycle.Host.csproj`. The regression sequence attaches host A, starts host B, minimizes B, closes A, and finally closes B. It verifies all visible owner PIDs, minimized-window presence, replacement survival, and final exit notification without interacting with the user's Codex process:

```powershell
dotnet build native/tests-lifecycle/host/QuotaFloat.Lifecycle.Host.csproj -c Release
dotnet run --project native/tests-lifecycle/QuotaFloat.Lifecycle.Tests.csproj -c Release
```
