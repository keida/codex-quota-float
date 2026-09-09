using System.IO;
using System.Text.Json;
using QuotaFloat;
using QuotaFloat.Wpf.Interaction;
using QuotaFloat.Wpf.Resources;
using QuotaFloat.Wpf.Services;
using QuotaFloat.Wpf.Platform;
using QuotaFloat.Wpf.Windows;

var failures = new List<string>();
var checkCount = 0;
void Check(bool condition, string label)
{
    checkCount++;
    if (!condition) failures.Add(label);
}

QuotaSnapshot? Parse(string usage, string credits = "{}")
{
    using var usageDocument = JsonDocument.Parse(usage);
    using var creditsDocument = JsonDocument.Parse(credits);
    return QuotaParser.Parse(usageDocument.RootElement, creditsDocument.RootElement, DateTimeOffset.UtcNow);
}

var both = Parse("""{"plan_type":"plus","rate_limit":{"primary_window":{"remaining_percent":72,"limit_window_seconds":18000},"secondary_window":{"remaining_percent":38,"limit_window_seconds":604800}}}""", """{"available_count":2,"credits":[{"expires_at":"2099-09-16T00:00:00Z"}]}""");
Check(both?.Short?.Remaining == 72 && both.Weekly?.Remaining == 38 && both.Credits == 2, "short+weekly fixture");

var weeklyOnly = Parse("""{"plan_type":"plus","rate_limit":{"primary_window":{"remaining_percent":98,"limit_window_seconds":604800}}}""", """{"available_count":1}""");
Check(weeklyOnly is not null && weeklyOnly.Short is null && weeklyOnly.Weekly is not null && weeklyOnly.VisibleWindows.Count == 1, "weekly-only has no synthetic short row");

var absent = Parse("""{"plan_type":"pro","rate_limit":{}}""");
Check(absent is null, "absent windows remain absent");
var unknown = Parse("""{"rate_limit":{"primary_window":{"remaining_percent":"unknown","limit_window_seconds":18000}}}""");
Check(unknown?.Short is null, "ambiguous fractional percent does not become quota zero");
var zero = Parse("""{"rate_limit":{"primary_window":{"remaining_percent":0,"limit_window_seconds":18000}}}""");
Check(zero?.Short?.Remaining == 0, "numeric zero remains numeric zero");
using var recognizedProSource = new FakeQuotaSource(new Queue<QuotaResult>(new[] { new QuotaResult(weeklyOnly!, "ok") }), TimeSpan.Zero);
using var recognizedProCoordinator = new QuotaRefreshCoordinator(recognizedProSource);
var recognizedPro = await recognizedProCoordinator.RefreshAsync();
Check(recognizedPro.Status == QuotaUiStatus.Fresh && recognizedPro.Snapshot is { Short: null, Weekly: not null },
    "recognized weekly-only snapshot remains Pro-compatible");
var expiryNow = DateTimeOffset.UtcNow;
var expirySnapshot = new QuotaSnapshot("Plus", null, null, 1, new[] { expiryNow.AddMinutes(-1), expiryNow.AddDays(2), expiryNow.AddDays(1) }, expiryNow);
Check(expirySnapshot.EarliestFutureExpiry(expiryNow) == expiryNow.AddDays(1), "earliest future reset expiry");

Check(QuotaDisplayState.LoadingState.Status == QuotaUiStatus.Loading && QuotaDisplayState.LoadingState.ActiveRequests == 1, "loading state is explicit");
Check(WidgetText.For("en-US").Status(QuotaUiStatus.Stale, true) == "Refreshing" && WidgetText.For("zh-Hans").Status(QuotaUiStatus.Stale, true) == "刷新中", "refreshing copy is localized");

var refreshingSnapshot = new QuotaSnapshot("Plus", new QuotaWindow(72, DateTimeOffset.UtcNow.AddHours(1), 18000, "5h"), new QuotaWindow(38, DateTimeOffset.UtcNow.AddDays(2), 604800, "Weekly"), 2, Array.Empty<DateTimeOffset>(), DateTimeOffset.UtcNow);
using var refreshingSource = new GateAfterFirstSource(refreshingSnapshot);
using var refreshingCoordinator = new QuotaRefreshCoordinator(refreshingSource);
var firstRefresh = await refreshingCoordinator.RefreshAsync();
var activeRefresh = refreshingCoordinator.RefreshAsync();
await refreshingSource.SecondStarted.Task;
var interimRefresh = refreshingCoordinator.State;
Check(firstRefresh.Status == QuotaUiStatus.Fresh && interimRefresh.IsRefreshing && interimRefresh.Status == QuotaUiStatus.Stale && ReferenceEquals(interimRefresh.Snapshot, refreshingSnapshot), "existing snapshot refresh retains quota while active");
refreshingSource.ReleaseSecond();
var completedRefresh = await activeRefresh;
Check(!completedRefresh.IsRefreshing && completedRefresh.Status == QuotaUiStatus.Fresh && ReferenceEquals(completedRefresh.Snapshot, refreshingSnapshot), "refresh completion replaces active state");

var signedOut = new Queue<QuotaResult>(new[] { new QuotaResult(null, "signedout") });
using var source = new FakeQuotaSource(signedOut, TimeSpan.FromMilliseconds(50));
using var coordinator = new QuotaRefreshCoordinator(source, TimeSpan.FromMilliseconds(100));
var repeated = await Task.WhenAll(coordinator.RefreshAsync(), coordinator.RefreshAsync(), coordinator.RefreshAsync());
Check(source.CallCount == 1 && source.MaxActive == 1 && repeated.All(item => item.Status == QuotaUiStatus.SignedOut), "repeated refresh coalesces to one request");

var cancellationGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
using var cancellationSource = new BlockingQuotaSource(cancellationGate.Task);
using var cancellationCoordinator = new QuotaRefreshCoordinator(cancellationSource);
using var manualCancellation = new CancellationTokenSource();
var manualRefresh = cancellationCoordinator.RefreshAsync(manualCancellation.Token);
await cancellationSource.Started.Task;
var automaticRefresh = cancellationCoordinator.RefreshAsync();
manualCancellation.Cancel();
var manualWasCancelled = false;
try { await manualRefresh; } catch (OperationCanceledException) { manualWasCancelled = true; }
cancellationGate.SetResult(true);
var automaticResult = await automaticRefresh;
Check(manualWasCancelled && automaticResult.Status == QuotaUiStatus.Fresh && cancellationSource.MaxActive == 1, "caller cancellation does not cancel shared refresh");

using var statusSource = new FakeQuotaSource(new Queue<QuotaResult>(new[]
{
    new QuotaResult(null, "ratelimit", TimeSpan.FromSeconds(20)),
    new QuotaResult(null, "format"),
    new QuotaResult(null, "sessionchanged")
}), TimeSpan.Zero);
using var statusCoordinator = new QuotaRefreshCoordinator(statusSource);
Check((await statusCoordinator.RefreshAsync()).Status == QuotaUiStatus.RateLimited, "rate limit status maps explicitly");
Check((await statusCoordinator.RefreshAsync()).Status == QuotaUiStatus.Malformed, "malformed status maps explicitly");
Check((await statusCoordinator.RefreshAsync()).Status == QuotaUiStatus.SessionChanged && statusCoordinator.State.Snapshot is null, "session change clears safe snapshot");

var unknownOnlySnapshot = new QuotaSnapshot("Plus", null, null, 1, Array.Empty<DateTimeOffset>(), DateTimeOffset.UtcNow,
    new[] { new QuotaWindow(61, DateTimeOffset.UtcNow.AddHours(3), 12345, "ServerWindow") });
using var unknownOnlySource = new FakeQuotaSource(new Queue<QuotaResult>(new[] { new QuotaResult(unknownOnlySnapshot, "ok") }), TimeSpan.Zero);
using var unknownOnlyCoordinator = new QuotaRefreshCoordinator(unknownOnlySource);
var unknownOnlyResult = await unknownOnlyCoordinator.RefreshAsync();
Check(unknownOnlySnapshot.VisibleWindows.Count == 1 && unknownOnlyResult.Status == QuotaUiStatus.Malformed && unknownOnlyResult.Snapshot is null,
    "unknown-only server bucket is rejected without synthetic Plus or Pro rows");

var shortOnlySnapshot = new QuotaSnapshot("Plus", new QuotaWindow(61, DateTimeOffset.UtcNow.AddHours(3), 18000, "5h"), null,
    1, Array.Empty<DateTimeOffset>(), DateTimeOffset.UtcNow,
    new[] { new QuotaWindow(61, DateTimeOffset.UtcNow.AddHours(3), 18000, "5h") });
using var shortOnlySource = new FakeQuotaSource(new Queue<QuotaResult>(new[] { new QuotaResult(shortOnlySnapshot, "ok") }), TimeSpan.Zero);
using var shortOnlyCoordinator = new QuotaRefreshCoordinator(shortOnlySource);
var shortOnlyResult = await shortOnlyCoordinator.RefreshAsync();
Check(shortOnlyResult.Status == QuotaUiStatus.Malformed && shortOnlyResult.Snapshot is null,
    "short-only server window is rejected without synthetic Weekly row");

using var signedOutAutoSource = new FakeQuotaSource(new Queue<QuotaResult>(new[]
{
    new QuotaResult(null, "signedout"),
    new QuotaResult(null, "offline")
}), TimeSpan.Zero);
using var signedOutAutoCoordinator = new QuotaRefreshCoordinator(signedOutAutoSource, TimeSpan.FromMilliseconds(25));
await signedOutAutoCoordinator.RunAutoRefreshAsync(() => TimeSpan.FromMilliseconds(1));
Check(signedOutAutoSource.CallCount == 1 && signedOutAutoCoordinator.State.Status == QuotaUiStatus.SignedOut, "signed-out auto refresh suspends without retry loop");

using var sessionAutoSource = new FakeQuotaSource(new Queue<QuotaResult>(new[]
{
    new QuotaResult(new QuotaSnapshot("Plus", new QuotaWindow(72, DateTimeOffset.UtcNow.AddHours(1), 18000, "5h"), new QuotaWindow(38, DateTimeOffset.UtcNow.AddDays(2), 604800, "Weekly"), 1, Array.Empty<DateTimeOffset>(), DateTimeOffset.UtcNow), "ok"),
    new QuotaResult(null, "sessionchanged"),
    new QuotaResult(new QuotaSnapshot("Plus", new QuotaWindow(64, DateTimeOffset.UtcNow.AddHours(1), 18000, "5h"), new QuotaWindow(38, DateTimeOffset.UtcNow.AddDays(2), 604800, "Weekly"), 1, Array.Empty<DateTimeOffset>(), DateTimeOffset.UtcNow), "ok")
}), TimeSpan.Zero);
using var sessionAutoCoordinator = new QuotaRefreshCoordinator(sessionAutoSource, TimeSpan.FromMilliseconds(25));
await sessionAutoCoordinator.RefreshAsync();
await sessionAutoCoordinator.RunAutoRefreshAsync(() => TimeSpan.FromMilliseconds(1));
Check(sessionAutoSource.CallCount == 2 && sessionAutoCoordinator.State.Status == QuotaUiStatus.SessionChanged && sessionAutoCoordinator.State.Snapshot is null, "session-changed auto refresh suspends and clears snapshot");
var manualAfterSession = await sessionAutoCoordinator.RefreshAsync();
Check(sessionAutoSource.CallCount == 3 && manualAfterSession.Status == QuotaUiStatus.Fresh && manualAfterSession.Snapshot is not null, "manual refresh remains available after terminal auto state");

var safe = new QuotaSnapshot("Plus", new QuotaWindow(72, expiryNow.AddHours(2), 18000, "5h"), new QuotaWindow(38, expiryNow.AddDays(2), 604800, "Weekly"), 2, Array.Empty<DateTimeOffset>(), expiryNow);
using var staleSource = new FakeQuotaSource(new Queue<QuotaResult>(new[] { new QuotaResult(safe, "ok"), new QuotaResult(null, "offline") }), TimeSpan.Zero);
using var staleCoordinator = new QuotaRefreshCoordinator(staleSource);
var fresh = await staleCoordinator.RefreshAsync();
var stale = await staleCoordinator.RefreshAsync();
Check(fresh.Status == QuotaUiStatus.Fresh && stale.Status == QuotaUiStatus.Stale && ReferenceEquals(stale.Snapshot, safe), "stale retains last safe snapshot and marks stale");

using var sessionSource = new FakeQuotaSource(new Queue<QuotaResult>(new[]
{
    new QuotaResult(safe, "ok"),
    new QuotaResult(null, "sessionchanged")
}), TimeSpan.Zero);
using var sessionCoordinator = new QuotaRefreshCoordinator(sessionSource);
await sessionCoordinator.RefreshAsync();
var sessionChanged = await sessionCoordinator.RefreshAsync();
Check(sessionChanged.Status == QuotaUiStatus.SessionChanged && sessionChanged.Snapshot is null, "session change does not retain prior account data");

using var backoffSource = new FakeQuotaSource(new Queue<QuotaResult>(Enumerable.Repeat(new QuotaResult(null, "offline"), 10)), TimeSpan.Zero);
using var backoffCoordinator = new QuotaRefreshCoordinator(backoffSource, TimeSpan.FromMilliseconds(100));
using var autoCancellation = new CancellationTokenSource(250);
await backoffCoordinator.RunAutoRefreshAsync(() => TimeSpan.FromMilliseconds(20), autoCancellation.Token);
Check(backoffSource.CallCount <= 4, "failure backoff prevents busy loop");

using var rateLimitBackoffSource = new FakeQuotaSource(new Queue<QuotaResult>(Enumerable.Repeat(new QuotaResult(null, "ratelimit", TimeSpan.FromMilliseconds(20)), 10)), TimeSpan.Zero);
using var rateLimitBackoffCoordinator = new QuotaRefreshCoordinator(rateLimitBackoffSource, TimeSpan.FromMilliseconds(100));
using var rateLimitCancellation = new CancellationTokenSource(250);
await rateLimitBackoffCoordinator.RunAutoRefreshAsync(() => TimeSpan.FromMilliseconds(20), rateLimitCancellation.Token);
Check(rateLimitBackoffSource.CallCount >= 2 && rateLimitBackoffSource.CallCount <= 4, "rate-limit retry remains bounded");

foreach (var interval in PreferenceStore.ApprovedRefreshIntervals)
{
    Check(PreferenceStore.NearestRefreshInterval(interval) == interval, $"approved refresh interval {interval}s remains exact");
}
Check(PreferenceStore.NearestRefreshInterval(0) == 30, "missing refresh interval defaults to 30s");
Check(PreferenceStore.NearestRefreshInterval(44) == 30, "refresh interval migrates to nearest approved value");
var migrationPath = Path.Combine(Path.GetTempPath(), $"quota-float-migration-{Guid.NewGuid():N}.json");
File.WriteAllText(migrationPath, "{\"Language\":\"en-US\",\"RefreshIntervalSeconds\":47,\"DisplayMode\":\"Orb\",\"FullSizePercent\":40,\"ClickThrough\":true,\"LowQuotaAlerts\":true}");
var migrated = PreferenceStore.Load(migrationPath);
Check(migrated.Current.Language == "en-US" && migrated.Current.RefreshIntervalSeconds == 60, "legacy preferences retain language and normalize interval");
migrated.Save();
var persisted = File.ReadAllText(migrationPath);
Check(!persisted.Contains("DisplayMode", StringComparison.Ordinal) && !persisted.Contains("ClickThrough", StringComparison.Ordinal) && !persisted.Contains("LowQuota", StringComparison.Ordinal), "removed preferences are not persisted");
File.Delete(migrationPath);
var appliedStates = new List<WidgetWindowState>();
var controller = new WidgetWindowController(
    new WidgetWindowState(WidgetPlacement.Free, TemporaryExpansion.None, WidgetPlan.Plus),
    appliedStates.Add);
Check(controller.DisplayMode == WidgetDisplayMode.Full, "free placement derives Full");
controller.SetPlacement(WidgetPlacement.Left);
Check(controller.DisplayMode == WidgetDisplayMode.Orb && controller.State.TemporaryExpansion == TemporaryExpansion.None, "edge placement derives Orb");
controller.SetTemporaryExpansion(TemporaryExpansion.Full);
Check(controller.DisplayMode == WidgetDisplayMode.Full && appliedStates.Count == 2, "temporary expansion derives Full through ApplyState");
controller.SetTemporaryExpansion(TemporaryExpansion.None);
Check(controller.DisplayMode == WidgetDisplayMode.Orb, "temporary expansion restore derives Orb");
Check(WidgetWindowController.DimensionsFor(new WidgetWindowState(WidgetPlacement.Free, TemporaryExpansion.None, WidgetPlan.Plus)) == new System.Windows.Size(278, 216), "Plus Full dimensions remain frozen");
Check(WidgetWindowController.DimensionsFor(new WidgetWindowState(WidgetPlacement.Right, TemporaryExpansion.None, WidgetPlan.Plus)) == new System.Windows.Size(74, 84), "Plus Orb dimensions remain frozen");
Check(WidgetWindowController.DimensionsFor(new WidgetWindowState(WidgetPlacement.Free, TemporaryExpansion.None, WidgetPlan.Pro)) == new System.Windows.Size(278, 156), "Pro Full dimensions remain frozen");
Check(WidgetWindowController.DimensionsFor(new WidgetWindowState(WidgetPlacement.Right, TemporaryExpansion.None, WidgetPlan.Pro)) == new System.Windows.Size(74, 62), "Pro Orb dimensions remain frozen");
var clippedRightBounds = new System.Windows.Rect(2549, 785, 278, 216);
var workArea = new System.Windows.Rect(0, 0, 2560, 1392);
var clippedRightSnapped = WindowPlacementService.TryGetEdgeSnap(
    clippedRightBounds, workArea, 96, new System.Windows.Size(74, 84), out var clippedRightEdge, out var clippedRightOrb);
Check(clippedRightSnapped && clippedRightEdge == OrbEdge.Right && clippedRightOrb.Right == workArea.Right,
    "edge snap recognizes a Full window dragged past the right work-area edge");
var leftMonitorWorkArea = new System.Windows.Rect(-2560, 0, 2560, 1392);
var leftMonitorBounds = new System.Windows.Rect(-4, 393, 278, 216);
var leftMonitorPlacement = new WindowPlacement((IntPtr)2, leftMonitorBounds, leftMonitorWorkArea, 96);
var leftMonitorSnapped = WindowPlacementService.TryGetEdgeSnap(
    leftMonitorBounds, leftMonitorWorkArea, 96, new System.Windows.Size(74, 84), out var leftMonitorEdge, out var leftMonitorOrb);
var primaryPlacement = new WindowPlacement((IntPtr)1, leftMonitorBounds, workArea, 96);
var primaryOrb = WindowPlacementService.AnchorOrb(primaryPlacement, OrbEdge.Left, new System.Windows.Size(74, 84));
var leftOrb = WindowPlacementService.AnchorOrb(leftMonitorPlacement, OrbEdge.Right, new System.Windows.Size(74, 84));
Check(leftMonitorSnapped && leftMonitorEdge == OrbEdge.Right && leftMonitorOrb.Right == leftMonitorWorkArea.Right,
    "left-monitor context detects the reached right edge from negative coordinates");
Check(leftOrb.Right == leftMonitorWorkArea.Right && primaryOrb.Left == workArea.Left && leftOrb.Left != primaryOrb.Left,
    "frozen monitor context keeps final Orb anchor on the selected monitor");
var leftSavedOrb = new SavedOrbPosition(new System.Windows.Rect(-74, 711, 74, 84), leftMonitorWorkArea, OrbEdge.Right, (IntPtr)2, 96);
var primarySavedOrb = new SavedOrbPosition(new System.Windows.Rect(2486, 711, 74, 84), workArea, OrbEdge.Right, (IntPtr)1, 96);
var leftSavedContext = WindowPlacementService.FromSavedOrb(leftSavedOrb);
var primarySavedContext = WindowPlacementService.FromSavedOrb(primarySavedOrb);
var leftTemporaryFull = OrbFullPlacement.ReanchorTemporaryFull(leftSavedOrb, leftSavedContext.WorkArea, new System.Windows.Size(278, 216));
var primaryTemporaryFull = OrbFullPlacement.ReanchorTemporaryFull(primarySavedOrb, primarySavedContext.WorkArea, new System.Windows.Size(278, 216));
Check(leftSavedContext.Monitor == (IntPtr)2 && leftTemporaryFull.Left == -278 && leftTemporaryFull.Right == 0,
    "saved left-monitor context expands Temporary Full inward on the same display");
Check(primarySavedContext.Monitor == (IntPtr)1 && primaryTemporaryFull.Left == 2282 && primaryTemporaryFull.Right == 2560,
    "saved primary context expands Temporary Full inward on the same display");
for (var cycle = 0; cycle < 3; cycle++)
{
    var restoredLeft = WindowPlacementService.AnchorOrb(leftSavedContext, leftSavedOrb.Edge, new System.Windows.Size(74, 84));
    var restoredPrimary = WindowPlacementService.AnchorOrb(primarySavedContext, primarySavedOrb.Edge, new System.Windows.Size(74, 84));
    Check(restoredLeft == leftSavedOrb.Bounds && restoredPrimary == primarySavedOrb.Bounds,
        $"saved Orb context restores exact coordinates on repeated cycle {cycle + 1}");
}

var cornerContract = WindowCornerContract.Civic;
Check(WindowCornerContract.CivicIdentity == "QF-CORNERS-DWM-ROUNDSMALL-B3-OPAQUE" &&
      cornerContract.NativeCornerPreference == 3 &&
      cornerContract.BorderColor == 0x003DADE2,
    "corner contract exposes the DWM small corner and native amber border");
var invalidStateRejected = false;
try { controller.ApplyState(new WidgetWindowState(WidgetPlacement.Free, TemporaryExpansion.Full, WidgetPlan.Plus)); }
catch (InvalidOperationException) { invalidStateRejected = true; }
Check(invalidStateRejected, "free plus temporary Full is structurally rejected");

var absence = new CodexAbsenceConfirmation(3);
Check(!absence.Observe(new(CodexObservationStatus.Unknown, 0, 0)), "failed scan is unknown");
Check(!absence.Observe(new(CodexObservationStatus.Absent, 0, 0)), "first absence is not confirmation");
Check(!absence.Observe(new(CodexObservationStatus.Absent, 0, 0)), "second absence is not confirmation");
Check(absence.Observe(new(CodexObservationStatus.Absent, 0, 0)), "third consecutive absence confirms close");
Check(!absence.Observe(new(CodexObservationStatus.Present, 1, 1)), "presence clears confirmation");

using (var primary = SingleInstanceService.Acquire(() => { }))
using (var secondary = SingleInstanceService.Acquire(() => { }))
{
    Check(primary.IsPrimary && !secondary.IsPrimary, "single instance elects one primary");
}

if (failures.Count > 0)
{
    foreach (var failure in failures) Console.WriteLine($"FAIL: {failure}");
    return 1;
}

Console.WriteLine("PASS: QF-WPF-009 focused fixtures and orchestration checks");
Console.WriteLine($"RESULTS: fixtures=31; checks={checkCount}; activeRequests={source.MaxActive}; refreshCalls={source.CallCount}; backoffCalls={backoffSource.CallCount}; privacy=normalized-values-only");
return 0;

sealed class FakeQuotaSource : IQuotaSource
{
    private readonly Queue<QuotaResult> results;
    private readonly TimeSpan delay;
    private int active;

    public FakeQuotaSource(Queue<QuotaResult> results, TimeSpan delay)
    {
        this.results = results;
        this.delay = delay;
    }

    public int CallCount { get; private set; }
    public int MaxActive { get; private set; }
    public long SessionGeneration { get; private set; }

    public async Task<QuotaResult> FetchAsync(CancellationToken cancellationToken)
    {
        CallCount++;
        var nowActive = Interlocked.Increment(ref active);
        MaxActive = Math.Max(MaxActive, nowActive);
        try
        {
            if (delay > TimeSpan.Zero) await Task.Delay(delay, cancellationToken);
            return results.Count > 0 ? results.Dequeue() : new QuotaResult(null, "offline");
        }
        finally { Interlocked.Decrement(ref active); }
    }

    public void Dispose() { }
}

sealed class GateAfterFirstSource : IQuotaSource
{
    private readonly QuotaSnapshot snapshot;
    private readonly TaskCompletionSource<bool> secondGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int calls;

    public GateAfterFirstSource(QuotaSnapshot snapshot) => this.snapshot = snapshot;
    public TaskCompletionSource<bool> SecondStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public long SessionGeneration => 1;

    public async Task<QuotaResult> FetchAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Increment(ref calls) == 2)
        {
            SecondStarted.TrySetResult(true);
            await secondGate.Task.WaitAsync(cancellationToken);
        }

        return new QuotaResult(snapshot, "ok");
    }

    public void ReleaseSecond() => secondGate.TrySetResult(true);
    public void Dispose() { }
}

sealed class BlockingQuotaSource : IQuotaSource
{
    private readonly Task gate;
    private readonly QuotaSnapshot snapshot = new("Plus", new QuotaWindow(72, DateTimeOffset.UtcNow.AddHours(1), 18000, "5h"), new QuotaWindow(38, DateTimeOffset.UtcNow.AddDays(2), 604800, "Weekly"), 1, Array.Empty<DateTimeOffset>(), DateTimeOffset.UtcNow);
    private int active;

    public BlockingQuotaSource(Task gate)
    {
        this.gate = gate;
        Started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public TaskCompletionSource<bool> Started { get; }
    public int MaxActive { get; private set; }
    public long SessionGeneration => 1;

    public async Task<QuotaResult> FetchAsync(CancellationToken cancellationToken)
    {
        var nowActive = Interlocked.Increment(ref active);
        MaxActive = Math.Max(MaxActive, nowActive);
        Started.TrySetResult(true);
        try
        {
            await gate.ConfigureAwait(false);
            return new(snapshot, "ok");
        }
        finally { Interlocked.Decrement(ref active); }
    }

    public void Dispose() { }
}
