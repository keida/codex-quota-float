using System.Diagnostics;
using System.Runtime.InteropServices;
using QuotaFloat;

if (args.Contains("--host", StringComparer.Ordinal))
{
    ApplicationConfiguration.Initialize();
    using var form = new Form
    {
        Text = "QuotaFloat lifecycle controlled host",
        Width = 320,
        Height = 160,
        ShowInTaskbar = true
    };
    Application.Run(form);
    return;
}

var passed = 0;
void Check(bool condition, string name)
{
    if (!condition)
    {
        throw new Exception("FAIL: " + name);
    }

    Console.WriteLine("PASS: " + name);
    passed++;
}

var hostPath = Path.GetFullPath(Path.Combine(
    AppContext.BaseDirectory,
    "..", "..", "..", "host", "bin", "Release", "net8.0-windows",
    "QuotaFloat.Lifecycle.Host.exe"));
if (!File.Exists(hostPath))
{
    throw new FileNotFoundException("Build the controlled host project beside this test project first.", hostPath);
}

using var first = StartHost(hostPath);
using var lifecycle = new CodexLifecycle();
var exited = 0;
lifecycle.DesktopExited += (_, _) => Interlocked.Increment(ref exited);
Check(await lifecycle.AttachTestProcessAsync(first.Id, CancellationToken.None), "attach first controlled host");
var initialPresence = lifecycle.Presence;
Check(initialPresence.IsPresent && initialPresence.ProcessIds.Contains(first.Id) && initialPresence.VisibleWindowCount >= 1, "presence reports the attached visible top-level window");

using var replacement = StartHost(hostPath);
var bothPresence = lifecycle.RefreshPresence();
Check(bothPresence.ProcessIds.Contains(first.Id) && bothPresence.ProcessIds.Contains(replacement.Id) && bothPresence.VisibleWindowCount >= 2, "presence tracks all matching visible desktop windows");
NativeLifecycleTestWindow.ShowWindow(replacement.MainWindowHandle, ShowWindowCommand.Minimize);
var minimizedPresence = lifecycle.RefreshPresence();
Check(minimizedPresence.IsPresent && minimizedPresence.ProcessIds.Contains(replacement.Id), "minimized desktop window remains present");
CloseAndWait(first);
await Task.Delay(250);
Check(exited == 0, "replacement host keeps lifecycle alive after first host exits");
await Task.Delay(850);
lifecycle.RefreshPresence();
Check(exited == 0, "replacement host still suppresses exit after confirmation grace");

CloseAndWait(replacement);
var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
while (Volatile.Read(ref exited) == 0 && DateTime.UtcNow < deadline)
{
    lifecycle.RefreshPresence();
    await Task.Delay(25);
}

Check(exited == 1, "lifecycle reports exit after final controlled host exits");
var absentPresence = lifecycle.RefreshPresence();
Check(!absentPresence.IsPresent && absentPresence.ProcessIds.Count == 0 && absentPresence.VisibleWindowCount == 0, "presence reports no visible desktop after final close");
Console.WriteLine($"{passed} lifecycle checks passed; controlled real host processes only.");

static Process StartHost(string hostPath)
{
    var process = Process.Start(new ProcessStartInfo
    {
        FileName = hostPath,
        UseShellExecute = true
    }) ?? throw new InvalidOperationException("Unable to start controlled host.");

    var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
    while (process.MainWindowHandle == IntPtr.Zero && DateTime.UtcNow < deadline)
    {
        process.Refresh();
        Thread.Sleep(25);
    }

    if (process.MainWindowHandle == IntPtr.Zero)
    {
        process.Kill();
        process.Dispose();
        throw new InvalidOperationException("Controlled host did not create a top-level window.");
    }

    return process;
}

static void CloseAndWait(Process process)
{
    if (process.HasExited)
    {
        return;
    }

    process.CloseMainWindow();
    if (!process.WaitForExit(5000))
    {
        process.Kill();
        process.WaitForExit(5000);
        throw new InvalidOperationException("Controlled host did not exit after close.");
    }
}

internal enum ShowWindowCommand
{
    Minimize = 6
}

internal static class NativeLifecycleTestWindow
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ShowWindow(IntPtr handle, ShowWindowCommand command);
}
