using QuotaFloat;

namespace QuotaFloat.Wpf.Services;

public sealed class QuotaRefreshCoordinator : IDisposable
{
    private readonly IQuotaSource source;
    private readonly object sync = new();
    private readonly CancellationTokenSource lifetime = new();
    private readonly TimeSpan maximumBackoff;
    private Task<QuotaDisplayState>? inFlight;
    private QuotaSnapshot? safeSnapshot;
    private DateTimeOffset? lastFreshAt;
    private QuotaDisplayState state = QuotaDisplayState.LoadingState;
    private bool disposed;

    public QuotaRefreshCoordinator(IQuotaSource source, TimeSpan? maximumBackoff = null)
    {
        this.source = source;
        this.maximumBackoff = maximumBackoff ?? TimeSpan.FromMinutes(15);
    }

    public event Action<QuotaDisplayState>? StateChanged;

    public QuotaDisplayState State
    {
        get { lock (sync) return state; }
    }

    public int ActiveRequests
    {
        get { lock (sync) return state.ActiveRequests; }
    }

    public Task<QuotaDisplayState> RefreshAsync(CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (inFlight is not null)
            {
                return AwaitWithCancellation(inFlight, cancellationToken);
            }

            var linked = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
            var task = RefreshCoreAsync(linked);
            inFlight = task;
            _ = task.ContinueWith(
                completed =>
                {
                    linked.Dispose();
                    lock (sync)
                    {
                        if (ReferenceEquals(inFlight, completed))
                        {
                            inFlight = null;
                        }
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            return AwaitWithCancellation(task, cancellationToken);
        }
    }

    private static async Task<QuotaDisplayState> AwaitWithCancellation(Task<QuotaDisplayState> shared, CancellationToken cancellationToken)
    {
        return await shared.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RunAutoRefreshAsync(Func<TimeSpan> intervalProvider, CancellationToken cancellationToken = default)
    {
        var failureCount = 0;
        TimeSpan? serverRetryAfter = null;
        while (!cancellationToken.IsCancellationRequested && !lifetime.IsCancellationRequested)
        {
            var delay = Backoff(intervalProvider(), failureCount, serverRetryAfter);
            serverRetryAfter = null;
            try
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                var result = await RefreshAsync(cancellationToken).ConfigureAwait(false);
                if (IsTerminalAutoStatus(result.Status))
                {
                    break;
                }

                if (result.Status is QuotaUiStatus.Fresh or QuotaUiStatus.Partial)
                {
                    failureCount = 0;
                }
                else
                {
                    failureCount = Math.Min(failureCount + 1, 8);
                    serverRetryAfter = result.RetryAfter;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || lifetime.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }
    }

    public void Cancel()
    {
        if (!lifetime.IsCancellationRequested)
        {
            lifetime.Cancel();
        }
    }

    public void Dispose()
    {
        lock (sync)
        {
            if (disposed) return;
            disposed = true;
        }

        Cancel();
        source.Dispose();
        lifetime.Dispose();
    }

    private async Task<QuotaDisplayState> RefreshCoreAsync(CancellationTokenSource linked)
    {
        Publish(CurrentWith(isRefreshing: true, activeRequests: 1, status: safeSnapshot is null ? QuotaUiStatus.Loading : QuotaUiStatus.Stale));
        try
        {
            var result = await source.FetchAsync(linked.Token).ConfigureAwait(false);
            QuotaDisplayState next;
            lock (sync)
            {
                if (result.Snapshot is { } snapshot && HasRenderableWindow(snapshot))
                {
                    safeSnapshot = snapshot;
                    lastFreshAt = snapshot.UpdatedAt;
                    var status = result.Status == "partial" ? QuotaUiStatus.Partial : QuotaUiStatus.Fresh;
                    next = new(status, safeSnapshot, null, lastFreshAt, null, false, 0, source.SessionGeneration);
                }
                else
                {
                    var failure = result.Snapshot is null ? MapFailure(result.Status) : QuotaUiStatus.Malformed;
                    var retainSafeSnapshot = CanRetainSafeSnapshot(failure);
                    if (!retainSafeSnapshot)
                    {
                        safeSnapshot = null;
                        lastFreshAt = null;
                    }

                    next = retainSafeSnapshot && safeSnapshot is not null
                        ? new(QuotaUiStatus.Stale, safeSnapshot, failure, lastFreshAt, result.RetryAfter, false, 0, source.SessionGeneration)
                        : new(failure, null, failure, lastFreshAt, result.RetryAfter, false, 0, source.SessionGeneration);
                }
            }

            Publish(next);
            return next;
        }
        catch (OperationCanceledException)
        {
            var cancelled = CurrentWith(false, 0, safeSnapshot is null ? QuotaUiStatus.Cancelled : QuotaUiStatus.Stale);
            Publish(cancelled);
            throw;
        }
        catch (Exception)
        {
            var failure = safeSnapshot is null ? QuotaUiStatus.Offline : QuotaUiStatus.Stale;
            var failed = CurrentWith(false, 0, failure) with { LastFailure = failure == QuotaUiStatus.Stale ? QuotaUiStatus.Offline : failure };
            Publish(failed);
            return failed;
        }
    }

    private QuotaDisplayState CurrentWith(bool isRefreshing, int activeRequests, QuotaUiStatus status)
    {
        lock (sync)
        {
            return state with
            {
                Status = status,
                Snapshot = safeSnapshot,
                IsRefreshing = isRefreshing,
                ActiveRequests = activeRequests,
                LastFreshAt = lastFreshAt,
                SessionGeneration = source.SessionGeneration
            };
        }
    }

    private void Publish(QuotaDisplayState next)
    {
        Action<QuotaDisplayState>? handler;
        lock (sync)
        {
            if (disposed) return;
            state = next;
            handler = StateChanged;
        }
        handler?.Invoke(next);
    }

    private TimeSpan Backoff(TimeSpan configured, int failureCount, TimeSpan? serverRetryAfter)
    {
        var safe = configured <= TimeSpan.Zero ? TimeSpan.FromSeconds(30) : configured;
        var multiplier = Math.Pow(2, Math.Min(failureCount, 8));
        var exponential = TimeSpan.FromMilliseconds(Math.Min(safe.TotalMilliseconds * multiplier, maximumBackoff.TotalMilliseconds));
        var requested = serverRetryAfter is { } retry && retry > TimeSpan.Zero ? retry : TimeSpan.Zero;
        var candidate = requested > exponential ? requested : exponential;
        if (candidate > maximumBackoff) candidate = maximumBackoff;
        return candidate <= TimeSpan.Zero ? TimeSpan.FromSeconds(30) : candidate;
    }

    private static bool CanRetainSafeSnapshot(QuotaUiStatus failure) =>
        failure is QuotaUiStatus.Offline or QuotaUiStatus.RateLimited;

    private static bool HasRenderableWindow(QuotaSnapshot snapshot) =>
        snapshot.Weekly is not null;

    private static bool IsTerminalAutoStatus(QuotaUiStatus status) =>
        status is QuotaUiStatus.SignedOut or QuotaUiStatus.SessionChanged;

    private static QuotaUiStatus MapFailure(string status) => status.ToLowerInvariant() switch
    {
        "signedout" => QuotaUiStatus.SignedOut,
        "offline" or "unavailable" => QuotaUiStatus.Offline,
        "ratelimit" => QuotaUiStatus.RateLimited,
        "sessionchanged" => QuotaUiStatus.SessionChanged,
        "format" or "malformed" => QuotaUiStatus.Malformed,
        _ => QuotaUiStatus.Offline
    };
}
