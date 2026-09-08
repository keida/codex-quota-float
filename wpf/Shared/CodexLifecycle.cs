using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Win32;

namespace QuotaFloat;

/// <summary>Read-only snapshot of official Codex desktop window presence.</summary>
public sealed class CodexPresence
{
    private readonly ReadOnlyCollection<int> _processIds;

    public CodexPresence(bool isPresent, int visibleWindowCount, IEnumerable<int> processIds)
    {
        IsPresent = isPresent;
        VisibleWindowCount = visibleWindowCount;
        _processIds = new ReadOnlyCollection<int>(processIds.Distinct().OrderBy(static id => id).ToArray());
    }

    public static CodexPresence Empty { get; } = new(false, 0, Array.Empty<int>());
    public bool IsPresent { get; }
    public int VisibleWindowCount { get; }
    public IReadOnlyList<int> ProcessIds => _processIds;
}

public sealed class CodexPresenceChangedEventArgs : EventArgs
{
    public CodexPresenceChangedEventArgs(CodexPresence previous, CodexPresence current)
    {
        Previous = previous;
        Current = current;
    }

    public CodexPresence Previous { get; }
    public CodexPresence Current { get; }
}

/// <summary>
/// Finds and observes the installed Codex desktop MSIX application.
/// This class never owns, closes, or terminates the desktop process.
/// </summary>
public sealed class CodexLifecycle : IDisposable
{
    private const string PackageName = "OpenAI.Codex";
    private const string DesktopProcessName = "ChatGPT";
    private const int WindowStyleIndex = -16;
    private const long WindowStyleCaption = 0x00C00000L;
    private const long WindowStyleThickFrame = 0x00040000L;
    private static readonly TimeSpan LaunchDiscoveryTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan LaunchDiscoveryInterval = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan ExitConfirmationDelay = TimeSpan.FromMilliseconds(750);

    private readonly object _sync = new();
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly Dictionary<int, Process> _observedProcesses = new();
    private readonly HashSet<string> _trackingExecutablePaths = new(StringComparer.OrdinalIgnoreCase);
    private string[] _cachedOfficialExecutablePaths = Array.Empty<string>();
    private DateTime _officialPathsCachedAtUtc;
    private bool _testProcessTracking;
    private Process? _desktopProcess;
    private int? _desktopProcessId;
    private CodexPresence _presence = CodexPresence.Empty;
    private string _status = "Not attached";
    private bool _disposed;

    public event EventHandler? DesktopExited;
    public event EventHandler<CodexPresenceChangedEventArgs>? PresenceChanged;

    public CodexPresence Presence
    {
        get
        {
            lock (_sync)
            {
                return _presence;
            }
        }
    }

    public int? DesktopProcessId
    {
        get
        {
            lock (_sync)
            {
                return _desktopProcessId;
            }
        }
    }

    public string Status
    {
        get
        {
            lock (_sync)
            {
                return _status;
            }
        }
    }

    /// <summary>
    /// Attaches to an already-running Codex desktop root process, or activates the
    /// installed MSIX application and waits briefly for its root process.
    /// Cancellation is propagated to the caller; a missing/unavailable app returns false.
    /// </summary>
    public async Task<bool> AttachOrLaunchAsync(bool launchIfMissing, CancellationToken cancellationToken)
    {
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _lifetimeCancellation.Token);

        await _operationGate.WaitAsync(linkedCancellation.Token).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            linkedCancellation.Token.ThrowIfCancellationRequested();

            var installed = FindInstalledApplication();
            if (installed is null)
            {
                SetStatus("Codex MSIX package not found");
                return false;
            }

            var officialPaths = FindOfficialExecutablePaths().DefaultIfEmpty(installed.ExecutablePath).ToArray();
            SetTrackingExecutablePaths(officialPaths);
            var existing = FindDesktopRoot(officialPaths);
            if (existing is not null)
            {
                return AttachProcess(existing, "Attached to Codex desktop");
            }

            if (!launchIfMissing)
            {
                SetStatus("Codex desktop is not running");
                return false;
            }

            SetStatus("Launching Codex desktop");
            if (!ActivateInstalledApplication(installed))
            {
                SetStatus("Codex MSIX activation failed");
                return false;
            }

            var deadline = DateTime.UtcNow + LaunchDiscoveryTimeout;
            while (DateTime.UtcNow < deadline)
            {
                linkedCancellation.Token.ThrowIfCancellationRequested();
                var process = FindDesktopRoot(officialPaths);
                if (process is not null)
                {
                    return AttachProcess(process, "Attached to Codex desktop");
                }

                await Task.Delay(LaunchDiscoveryInterval, linkedCancellation.Token).ConfigureAwait(false);
            }

            SetStatus("Codex desktop did not start within 20 seconds");
            return false;
        }
        catch (OperationCanceledException)
        {
            SetStatus("Codex lifecycle operation cancelled");
            throw;
        }
        finally
        {
            _operationGate.Release();
        }
    }

    /// <summary>
    /// Attaches only to a caller-supplied temporary test process. The caller must
    /// start that process in the explicit <c>--test-parent</c> mode. This method is
    /// intentionally not used by production process discovery and does not inspect
    /// or print command lines.
    /// </summary>
    public async Task<bool> AttachTestProcessAsync(int processId, CancellationToken cancellationToken)
    {
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _lifetimeCancellation.Token);

        await _operationGate.WaitAsync(linkedCancellation.Token).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            linkedCancellation.Token.ThrowIfCancellationRequested();

            Process process;
            try
            {
                process = Process.GetProcessById(processId);
                if (process.HasExited)
                {
                    process.Dispose();
                    SetStatus("Test process has already exited");
                    return false;
                }
            }
            catch (ArgumentException)
            {
                SetStatus("Test process was not found");
                return false;
            }
            catch (InvalidOperationException)
            {
                SetStatus("Test process has already exited");
                return false;
            }

            string executablePath;
            try
            {
                executablePath = process.MainModule?.FileName ?? string.Empty;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                process.Dispose();
                SetStatus("Test process executable is unavailable");
                return false;
            }

            if (string.IsNullOrWhiteSpace(executablePath))
            {
                process.Dispose();
                SetStatus("Test process executable is unavailable");
                return false;
            }

            SetTrackingExecutablePaths(new[] { executablePath }, testProcess: true);
            return AttachProcess(process, "Attached to test parent process");
        }
        catch (OperationCanceledException)
        {
            SetStatus("Codex lifecycle operation cancelled");
            throw;
        }
        finally
        {
            _operationGate.Release();
        }
    }

    internal void PrepareTestProcessTracking()
    {
        SetTrackingExecutablePaths(new[] { Path.Combine(Path.GetTempPath(), "QuotaFloat-no-test-host.exe") }, true);
    }

    public void Dispose()
    {
        List<Process> processes;
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _lifetimeCancellation.Cancel();
            processes = _observedProcesses.Values.ToList();
            _observedProcesses.Clear();
            _desktopProcess = null;
            _desktopProcessId = null;
            foreach (var process in processes)
            {
                process.Exited -= OnObservedProcessExited;
            }
        }

        foreach (var process in processes)
        {
            process.Dispose();
        }

    }

    private bool AttachProcess(Process process, string attachedStatus)
    {
        List<Process> previous;
        var keepProcess = false;
        lock (_sync)
        {
            if (_disposed)
            {
                process.Dispose();
                return false;
            }

            previous = _observedProcesses.Values
                .Where(existing => !ReferenceEquals(existing, process))
                .ToList();
            foreach (var existing in previous)
            {
                existing.Exited -= OnObservedProcessExited;
            }

            _observedProcesses.Clear();

            _desktopProcess = process;
            try
            {
                _desktopProcessId = process.Id;
                _status = attachedStatus;
                process.Exited += OnObservedProcessExited;
                _observedProcesses[process.Id] = process;
                process.EnableRaisingEvents = true;

                if (process.HasExited)
                {
                    process.Exited -= OnObservedProcessExited;
                    _observedProcesses.Remove(process.Id);
                    _desktopProcess = null;
                    _desktopProcessId = null;
                    _status = "Desktop process exited";
                }
                else
                {
                    keepProcess = true;
                }
            }
            catch (InvalidOperationException)
            {
                if (ReferenceEquals(_desktopProcess, process))
                {
                    process.Exited -= OnObservedProcessExited;
                    _observedProcesses.Remove(process.Id);
                    _desktopProcess = null;
                    _desktopProcessId = null;
                    _status = "Desktop process exited";
                }

            }
            catch (System.ComponentModel.Win32Exception)
            {
                if (ReferenceEquals(_desktopProcess, process))
                {
                    process.Exited -= OnObservedProcessExited;
                    _observedProcesses.Remove(process.Id);
                    _desktopProcess = null;
                    _desktopProcessId = null;
                    _status = "Desktop process is unavailable";
                }

            }
        }

        foreach (var oldProcess in previous)
        {
            oldProcess.Dispose();
        }

        if (keepProcess)
        {
            RefreshPresence();
        }
        else
        {
            process.Dispose();
        }

        return keepProcess;
    }

    /// <summary>
    /// Re-enumerates official desktop windows and processes. The caller owns any
    /// polling cadence; this method does not install a permanent watcher.
    /// </summary>
    public CodexPresence RefreshPresence()
    {
        string[] paths;
        lock (_sync)
        {
            if (_disposed)
            {
                return _presence;
            }

            paths = _trackingExecutablePaths.ToArray();
            if (!_testProcessTracking &&
                (paths.Length == 0 || DateTime.UtcNow - _officialPathsCachedAtUtc >= TimeSpan.FromSeconds(30)))
            {
                paths = Array.Empty<string>();
            }
        }

        if (paths.Length == 0)
        {
            paths = GetCachedOfficialExecutablePaths();
            SetTrackingExecutablePaths(paths);
        }

        var processes = FindProcessesByExecutablePaths(paths);
        var visibleWindowProcessIds = EnumerateVisibleWindowProcessIds();
        var visibleProcessIds = processes
            .Where(process => visibleWindowProcessIds.ContainsKey(process.Key))
            .Select(static process => process.Key)
            .ToArray();
        var snapshot = new CodexPresence(
            visibleProcessIds.Length > 0,
            visibleWindowProcessIds.Where(pair => visibleProcessIds.Contains(pair.Key)).Sum(static pair => pair.Value),
            visibleProcessIds);

        CodexPresenceChangedEventArgs? changed = null;
        EventHandler<CodexPresenceChangedEventArgs>? presenceChanged;
        lock (_sync)
        {
            if (_disposed)
            {
                foreach (var process in processes.Values)
                {
                    process.Dispose();
                }
                return _presence;
            }

            foreach (var candidate in processes.Values)
            {
                if (_observedProcesses.ContainsKey(candidate.Id))
                {
                    candidate.Dispose();
                    continue;
                }

                try
                {
                    candidate.Exited += OnObservedProcessExited;
                    _observedProcesses[candidate.Id] = candidate;
                    candidate.EnableRaisingEvents = true;
                }
                catch (InvalidOperationException)
                {
                    _observedProcesses.Remove(candidate.Id);
                    candidate.Exited -= OnObservedProcessExited;
                    candidate.Dispose();
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    _observedProcesses.Remove(candidate.Id);
                    candidate.Exited -= OnObservedProcessExited;
                    candidate.Dispose();
                }
            }
            // Keep absent handles subscribed until their Exited callback runs.
            // Removing them here can race and suppress the exit notification.

            if (_desktopProcess is null && _observedProcesses.Count > 0)
            {
                var replacement = SelectRootProcess(_observedProcesses.Values);
                _desktopProcess = replacement;
                _desktopProcessId = replacement.Id;
                _status = "Codex desktop process replaced";
            }

            if (!PresenceEquals(_presence, snapshot))
            {
                var previous = _presence;
                _presence = snapshot;
                changed = new CodexPresenceChangedEventArgs(previous, snapshot);
            }
            else
            {
                _presence = snapshot;
            }

            presenceChanged = PresenceChanged;
        }

        if (changed is not null && presenceChanged is not null)
        {
            presenceChanged(this, changed);
        }
        return snapshot;
    }

    private void OnObservedProcessExited(object? sender, EventArgs e)
    {
        if (sender is not Process process)
        {
            return;
        }

        bool wasDesktopProcess;
        lock (_sync)
        {
            if (_disposed || !_observedProcesses.TryGetValue(process.Id, out var observed) || !ReferenceEquals(observed, process))
            {
                return;
            }
            _observedProcesses.Remove(process.Id);

            wasDesktopProcess = ReferenceEquals(_desktopProcess, process);
            if (wasDesktopProcess)
            {
                _desktopProcess = null;
                _desktopProcessId = null;
            }

            process.Exited -= OnObservedProcessExited;
        }

        _ = ConfirmProcessExitAsync(process, wasDesktopProcess);
    }

    private async Task ConfirmProcessExitAsync(Process process, bool wasDesktopProcess)
    {
        try
        {
            await Task.Delay(ExitConfirmationDelay, _lifetimeCancellation.Token).ConfigureAwait(false);
            var snapshot = RefreshPresence();
            if (!wasDesktopProcess || snapshot.ProcessIds.Count > 0 || HasMatchingProcesses(GetTrackingPaths()))
            {
                process.Dispose();
                return;
            }

            EventHandler? exited;
            lock (_sync)
            {
                if (_disposed || _observedProcesses.Count > 0)
                {
                    process.Dispose();
                    return;
                }

                _desktopProcess = null;
                _desktopProcessId = null;
                _status = "Desktop process exited";
                exited = DesktopExited;
            }

            process.Dispose();
            exited?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            process.Dispose();
        }
    }

    private string[] GetTrackingPaths()
    {
        lock (_sync)
        {
            return _trackingExecutablePaths.ToArray();
        }
    }

    private bool HasMatchingProcesses(IEnumerable<string> paths)
    {
        var processes = FindProcessesByExecutablePaths(paths);
        foreach (var process in processes.Values)
        {
            process.Dispose();
        }

        return processes.Count > 0;
    }

    private string[] GetCachedOfficialExecutablePaths()
    {
        lock (_sync)
        {
            if (_officialPathsCachedAtUtc != DateTime.MinValue &&
                DateTime.UtcNow - _officialPathsCachedAtUtc < TimeSpan.FromSeconds(30))
            {
                return _cachedOfficialExecutablePaths.ToArray();
            }
        }

        var discovered = FindOfficialExecutablePaths().Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        lock (_sync)
        {
            if (!_disposed)
            {
                _cachedOfficialExecutablePaths = discovered;
                _officialPathsCachedAtUtc = DateTime.UtcNow;
            }

            return discovered;
        }
    }

    private void SetTrackingExecutablePaths(IEnumerable<string> paths, bool testProcess = false)
    {
        lock (_sync)
        {
            _testProcessTracking = testProcess;
            _trackingExecutablePaths.Clear();
            foreach (var path in paths.Where(static path => !string.IsNullOrWhiteSpace(path)))
            {
                _trackingExecutablePaths.Add(Path.GetFullPath(path));
            }
        }
    }

    private static bool PresenceEquals(CodexPresence left, CodexPresence right)
    {
        return left.IsPresent == right.IsPresent &&
            left.VisibleWindowCount == right.VisibleWindowCount &&
            left.ProcessIds.SequenceEqual(right.ProcessIds);
    }

    private void SetStatus(string status)
    {
        lock (_sync)
        {
            if (!_disposed)
            {
                _status = status;
            }
        }
    }

    private void ThrowIfDisposed()
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
    }

    private static bool ActivateInstalledApplication(InstalledApplication app)
    {
        try
        {
            using var activation = Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"shell:AppsFolder\\{app.Aumid}",
                UseShellExecute = true,
                CreateNoWindow = true
            });
            return activation is not null;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    private static Dictionary<int, Process> FindProcessesByExecutablePaths(IEnumerable<string> executablePaths)
    {
        var paths = executablePaths
            .Select(Path.GetFullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var processes = new Dictionary<int, Process>();
        foreach (var processName in paths.Select(Path.GetFileNameWithoutExtension).Where(static name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            foreach (var process in Process.GetProcessesByName(processName!))
            {
                try
                {
                    if (process.HasExited || process.MainModule?.FileName is not { } actualPath || !paths.Contains(actualPath))
                    {
                        process.Dispose();
                        continue;
                    }

                    if (!processes.TryAdd(process.Id, process))
                    {
                        process.Dispose();
                    }
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    process.Dispose();
                }
                catch (InvalidOperationException)
                {
                    process.Dispose();
                }
            }
        }

        return processes;
    }

    private static Dictionary<int, int> EnumerateVisibleWindowProcessIds()
    {
        var windowsByProcess = new Dictionary<int, int>();
        EnumWindows((window, _) =>
        {
            if (!IsWindowVisible(window))
            {
                return true;
            }

            if (!IsEligibleDesktopWindow(window))
            {
                return true;
            }

            GetWindowThreadProcessId(window, out var processId);
            if (processId != 0)
            {
                windowsByProcess[unchecked((int)processId)] = windowsByProcess.GetValueOrDefault(unchecked((int)processId)) + 1;
            }

            return true;
        }, IntPtr.Zero);
        return windowsByProcess;
    }

    private static bool IsEligibleDesktopWindow(IntPtr window)
    {
        var style = GetWindowLongPtr(window, WindowStyleIndex).ToInt64();
        return (style & WindowStyleCaption) == WindowStyleCaption &&
            (style & WindowStyleThickFrame) == WindowStyleThickFrame;
    }

    private static IEnumerable<string> FindOfficialExecutablePaths()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var packageRoot in EnumeratePackageRoots())
        {
            var manifestPath = Path.Combine(packageRoot.Root, "AppxManifest.xml");
            try
            {
                if (!IsOfficialPackageRoot(packageRoot.Root) || !File.Exists(manifestPath))
                {
                    continue;
                }

                var manifest = XDocument.Load(manifestPath, LoadOptions.PreserveWhitespace);
                var ns = manifest.Root?.GetDefaultNamespace() ?? XNamespace.None;
                var identity = manifest.Root?.Element(ns + "Identity");
                if (!string.Equals(identity?.Attribute("Name")?.Value, PackageName, StringComparison.Ordinal))
                {
                    continue;
                }

                var executable = manifest.Root?.Element(ns + "Applications")?.Elements(ns + "Application")
                    .Select(application => application.Attribute("Executable")?.Value)
                    .FirstOrDefault(value => string.Equals(value, "app/ChatGPT.exe", StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrWhiteSpace(executable))
                {
                    continue;
                }

                var executablePath = Path.GetFullPath(Path.Combine(packageRoot.Root, executable.Replace('/', Path.DirectorySeparatorChar)));
                if (IsWithin(packageRoot.Root, executablePath) && File.Exists(executablePath))
                {
                    paths.Add(executablePath);
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (IOException)
            {
            }
            catch (XmlException)
            {
            }
        }

        return paths;
    }

    private static Process? FindDesktopRoot(string executablePath)
        => FindDesktopRoot(new[] { executablePath });

    private static Process? FindDesktopRoot(IEnumerable<string> executablePaths)
    {
        var paths = executablePaths.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var matching = new List<Process>();
        try
        {
            foreach (var process in Process.GetProcessesByName(DesktopProcessName))
            {
                try
                {
                    if (process.MainModule?.FileName is { } actualPath && paths.Contains(actualPath))
                    {
                        matching.Add(process);
                    }
                    else
                    {
                        process.Dispose();
                    }
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    process.Dispose();
                }
                catch (InvalidOperationException)
                {
                    process.Dispose();
                }
            }

            if (matching.Count == 0)
            {
                return null;
            }

            var matchingIds = matching.Select(static p => p.Id).ToHashSet();
            var roots = matching
                .Where(p => !matchingIds.Contains(GetParentProcessId(p.Id)))
                .ToList();
            var selected = (roots.Count > 0 ? roots : matching)
                .OrderBy(GetStartTimeOrMax)
                .First();

            foreach (var process in matching)
            {
                if (!ReferenceEquals(process, selected))
                {
                    process.Dispose();
                }
            }

            return selected;
        }
        catch
        {
            foreach (var process in matching)
            {
                process.Dispose();
            }

            return null;
        }
    }

    private static DateTime GetStartTimeOrMax(Process process)
    {
        try
        {
            return process.StartTime;
        }
        catch
        {
            return DateTime.MaxValue;
        }
    }

    private static Process SelectRootProcess(IEnumerable<Process> processes)
    {
        var candidates = processes.ToList();
        var ids = candidates.Select(static process => process.Id).ToHashSet();
        return candidates
            .Where(process => !ids.Contains(GetParentProcessId(process.Id)))
            .DefaultIfEmpty(candidates[0])
            .OrderBy(GetStartTimeOrMax)
            .First();
    }

    private static int GetParentProcessId(int processId)
    {
        using var snapshot = SafeHandle.Create(CreateToolhelp32Snapshot(SnapshotFlags.Process, 0));
        if (snapshot.IsInvalid)
        {
            return -1;
        }

        var entry = new ProcessEntry32 { Size = (uint)Marshal.SizeOf<ProcessEntry32>() };
        if (!Process32First(snapshot, ref entry))
        {
            return -1;
        }

        do
        {
            if (entry.ProcessId == processId)
            {
                return unchecked((int)entry.ParentProcessId);
            }
        }
        while (Process32Next(snapshot, ref entry));

        return -1;
    }

    private static InstalledApplication? FindInstalledApplication()
    {
        var candidates = new List<InstalledApplication>();
        foreach (var packageRoot in EnumeratePackageRoots())
        {
            var manifestPath = Path.Combine(packageRoot.Root, "AppxManifest.xml");
            try
            {
                if (!IsOfficialPackageRoot(packageRoot.Root) || !File.Exists(manifestPath))
                {
                    continue;
                }

                var manifest = XDocument.Load(manifestPath, LoadOptions.PreserveWhitespace);
                var ns = manifest.Root?.GetDefaultNamespace() ?? XNamespace.None;
                var identity = manifest.Root?.Element(ns + "Identity");
                var applications = manifest.Root?.Element(ns + "Applications");
                var application = applications?.Elements(ns + "Application")
                    .FirstOrDefault(a => string.Equals(a.Attribute("Executable")?.Value, "app/ChatGPT.exe", StringComparison.OrdinalIgnoreCase));
                var identityName = identity?.Attribute("Name")?.Value;
                var packageId = packageRoot.PackageId;
                var appId = application?.Attribute("Id")?.Value;
                var executable = application?.Attribute("Executable")?.Value;
                if (!string.Equals(identityName, PackageName, StringComparison.Ordinal) ||
                    string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(executable))
                {
                    continue;
                }

                var publisherSeparator = packageId.LastIndexOf("__", StringComparison.Ordinal);
                if (publisherSeparator <= 0 || publisherSeparator + 2 >= packageId.Length)
                {
                    continue;
                }

                var familyName = $"{identityName}_{packageId[(publisherSeparator + 2)..]}";
                var executablePath = Path.GetFullPath(Path.Combine(packageRoot.Root, executable.Replace('/', Path.DirectorySeparatorChar)));
                if (!IsWithin(packageRoot.Root, executablePath) || !File.Exists(executablePath))
                {
                    continue;
                }

                var version = identity?.Attribute("Version")?.Value ?? "0.0.0.0";
                candidates.Add(new InstalledApplication(executablePath, $"{familyName}!{appId}", version));
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (IOException)
            {
            }
            catch (XmlException)
            {
            }
        }

        return candidates
            .OrderByDescending(static c => Version.TryParse(c.Version, out var parsed) ? parsed : new Version(0, 0))
            .FirstOrDefault();
    }

    private static IEnumerable<PackageRoot> EnumeratePackageRoots()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var roots = new List<PackageRoot>();
        const string repositoryPath = "Software\\Classes\\Local Settings\\Software\\Microsoft\\Windows\\CurrentVersion\\AppModel\\Repository\\Packages";
        foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
                using var packages = baseKey.OpenSubKey(repositoryPath);
                if (packages is null)
                {
                    continue;
                }

                foreach (var name in packages.GetSubKeyNames().Where(static n => n.StartsWith(PackageName + "_", StringComparison.OrdinalIgnoreCase)))
                {
                    using var package = packages.OpenSubKey(name);
                    var root = package?.GetValue("PackageRootFolder") as string;
                    var packageId = package?.GetValue("PackageID") as string ?? name;
                    if (!string.IsNullOrWhiteSpace(root) && seen.Add(root))
                    {
                        roots.Add(new PackageRoot(root, packageId));
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (IOException)
            {
            }
        }

        foreach (var windowsApps in GetWindowsAppsRoots())
        {
            string[] directories;
            try
            {
                directories = Directory.EnumerateDirectories(windowsApps, PackageName + "_*").ToArray();
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (DirectoryNotFoundException)
            {
                continue;
            }

            foreach (var root in directories)
            {
                var packageId = Path.GetFileName(root);
                if (seen.Add(root))
                {
                    roots.Add(new PackageRoot(root, packageId));
                }
            }
        }

        return roots;
    }

    private static IEnumerable<string> GetWindowsAppsRoots()
    {
        foreach (var programFiles in new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetEnvironmentVariable("ProgramFiles(x86)")
        }.Where(static p => !string.IsNullOrWhiteSpace(p)))
        {
            yield return Path.Combine(programFiles!, "WindowsApps");
        }
    }

    private static bool IsOfficialPackageRoot(string root)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return GetWindowsAppsRoots().Any(windowsApps =>
        {
            var fullWindowsApps = Path.GetFullPath(windowsApps).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return fullRoot.StartsWith(fullWindowsApps, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static bool IsWithin(string root, string path)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(path);
        return fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record InstalledApplication(string ExecutablePath, string Aumid, string Version);
    private sealed record PackageRoot(string Root, string PackageId);

    [Flags]
    private enum SnapshotFlags : uint
    {
        Process = 0x00000002
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ProcessEntry32
    {
        public uint Size;
        public uint Usage;
        public uint ProcessId;
        private IntPtr _defaultHeapId;
        private uint _moduleId;
        private uint _threads;
        private uint _parentProcessId;
        private int _basePriority;
        private uint _flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        private string _exeFile;

        public uint ParentProcessId => _parentProcessId;
    }

    private sealed class SafeHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeHandle() : base(true) { }

        public static SafeHandle Create(IntPtr handle)
        {
            var safe = new SafeHandle();
            safe.SetHandle(handle);
            return safe;
        }

        protected override bool ReleaseHandle() => CloseHandle(handle);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateToolhelp32Snapshot(SnapshotFlags flags, uint processId);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Process32First(SafeHandle snapshot, ref ProcessEntry32 entry);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Process32Next(SafeHandle snapshot, ref ProcessEntry32 entry);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

    private delegate bool EnumWindowsProc(IntPtr window, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr window);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
}
