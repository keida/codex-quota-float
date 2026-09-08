using QuotaFloat;

namespace QuotaFloat.Wpf.Services;

public enum QuotaUiStatus
{
    Loading,
    Fresh,
    Partial,
    Stale,
    SignedOut,
    Offline,
    RateLimited,
    Malformed,
    SessionChanged,
    Cancelled
}

public sealed record QuotaDisplayState(
    QuotaUiStatus Status,
    QuotaSnapshot? Snapshot,
    QuotaUiStatus? LastFailure,
    DateTimeOffset? LastFreshAt,
    TimeSpan? RetryAfter,
    bool IsRefreshing,
    int ActiveRequests,
    long SessionGeneration)
{
    public static QuotaDisplayState LoadingState => new(QuotaUiStatus.Loading, null, null, null, null, true, 1, 0);

    public bool HasSafeSnapshot => Snapshot is not null;

    public string SyncKey => Status switch
    {
        QuotaUiStatus.Loading => "loading",
        QuotaUiStatus.Fresh => "fresh",
        QuotaUiStatus.Partial => "partial",
        QuotaUiStatus.Stale => "stale",
        QuotaUiStatus.SignedOut => "signedout",
        QuotaUiStatus.Offline => "offline",
        QuotaUiStatus.RateLimited => "ratelimit",
        QuotaUiStatus.Malformed => "malformed",
        QuotaUiStatus.SessionChanged => "sessionchanged",
        _ => "cancelled"
    };
}

public interface IQuotaSource : IDisposable
{
    long SessionGeneration { get; }
    Task<QuotaResult> FetchAsync(CancellationToken cancellationToken);
}

public sealed class QuotaClientSource : IQuotaSource
{
    private readonly QuotaClient client = new();

    public long SessionGeneration => client.SessionGeneration;

    public Task<QuotaResult> FetchAsync(CancellationToken cancellationToken) => client.FetchAsync(cancellationToken);

    public void Dispose() => client.Dispose();
}

