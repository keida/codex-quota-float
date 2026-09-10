using QuotaFloat;

namespace QuotaFloat.Wpf.Services;

public enum CodexObservationStatus
{
    Present,
    Absent,
    Unknown
}

public sealed record CodexObservation(CodexObservationStatus Status, int VisibleWindows, int ProcessCount);

public sealed class CodexAbsenceConfirmation
{
    private readonly int requiredSamples;
    private int consecutiveAbsences;

    public CodexAbsenceConfirmation(int requiredSamples = 3)
    {
        if (requiredSamples < 1) throw new ArgumentOutOfRangeException(nameof(requiredSamples));
        this.requiredSamples = requiredSamples;
    }

    public int ConsecutiveAbsences => consecutiveAbsences;
    public bool IsConfirmed { get; private set; }

    public bool Observe(CodexObservation observation)
    {
        if (observation.Status != CodexObservationStatus.Absent)
        {
            consecutiveAbsences = 0;
            IsConfirmed = false;
            return false;
        }

        consecutiveAbsences++;
        IsConfirmed = consecutiveAbsences >= requiredSamples;
        return IsConfirmed;
    }
}

public interface ICodexPresenceSource : IDisposable
{
    Task<CodexObservation> AttachAndSampleAsync(bool launchIfMissing, CancellationToken cancellationToken);
    Task<CodexObservation> SampleAsync(bool launchIfMissing, CancellationToken cancellationToken);
}

public sealed class CodexLifecyclePresenceSource : ICodexPresenceSource
{
    private readonly CodexLifecycle lifecycle = new();
    private bool attached;

    public async Task<CodexObservation> AttachAndSampleAsync(bool launchIfMissing, CancellationToken cancellationToken)
    {
        try
        {
            attached = await lifecycle.AttachOrLaunchAsync(launchIfMissing, cancellationToken).ConfigureAwait(false);
            if (!attached)
            {
                return lifecycle.Status.Contains("not running", StringComparison.OrdinalIgnoreCase)
                    ? new(CodexObservationStatus.Absent, 0, 0)
                    : new(CodexObservationStatus.Unknown, 0, 0);
            }
            return ToObservation(lifecycle.RefreshPresence());
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception)
        {
            attached = false;
            return new(CodexObservationStatus.Unknown, 0, 0);
        }
    }

    public async Task<CodexObservation> SampleAsync(bool launchIfMissing, CancellationToken cancellationToken)
    {
        if (!attached) return await AttachAndSampleAsync(launchIfMissing, cancellationToken).ConfigureAwait(false);
        try { return ToObservation(lifecycle.RefreshPresence()); }
        catch (Exception)
        {
            attached = false;
            return new(CodexObservationStatus.Unknown, 0, 0);
        }
    }

    public void Dispose() => lifecycle.Dispose();

    private static CodexObservation ToObservation(CodexPresence presence) =>
        new(presence.IsPresent ? CodexObservationStatus.Present : CodexObservationStatus.Absent,
            presence.VisibleWindowCount,
            presence.ProcessIds.Count);
}
