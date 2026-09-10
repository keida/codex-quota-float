using System.Windows;
using System.Windows.Threading;
using QuotaFloat;
using QuotaFloat.Wpf.Windows;

namespace QuotaFloat.Wpf.Services;

public sealed class WpfApplicationCoordinator : IDisposable
{
    private static readonly TimeSpan PresenceInterval = TimeSpan.FromSeconds(1);
    private const int RequiredConsecutiveAbsences = 3;

    private readonly Dispatcher dispatcher;
    private readonly CancellationTokenSource lifetime = new();
    private SingleInstanceService? singleInstance;
    private QuotaRefreshCoordinator? quota;
    private ICodexPresenceSource? codex;
    private MainWindow? window;
    private CancellationTokenSource? autoRefreshLifetime;
    private bool stopping;
    private bool disposed;
    private bool launchCodexIfMissing;

    public WpfApplicationCoordinator(Dispatcher dispatcher)
    {
        this.dispatcher = dispatcher;
    }

    public MainWindow? Window => window;
    public QuotaRefreshCoordinator? Quota => quota;
    public WpfLaunchMode LaunchMode { get; private set; } = WpfLaunchMode.LaunchAndWatch;

    public bool Start(string[] args)
    {
        singleInstance = SingleInstanceService.Acquire(ActivateExistingInstance);
        if (!singleInstance.IsPrimary)
        {
            singleInstance.Dispose();
            return false;
        }

        var preferences = PreferenceStore.Load();
        if (ParseDemoLanguage(args) is { } demoLanguage)
        {
            preferences.Current.Language = demoLanguage;
        }
        var options = LaunchOptions.Parse(args);
        if (!options.IsValid)
        {
            MessageBox.Show(options.Error!, "Quote Float", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        LaunchMode = options.Mode;
        launchCodexIfMissing = options.LaunchCodexIfMissing;
        var demoStatus = ParseDemoStatus(args);
        var demoMode = args.Any(a => string.Equals(a, "--demo-state", StringComparison.OrdinalIgnoreCase)) || demoStatus is not null;
        quota = demoMode ? null : new QuotaRefreshCoordinator(new QuotaClientSource());
        codex = demoMode ? null : new CodexLifecyclePresenceSource();

        var demo = ParseDemoState(args);
        window = new MainWindow(
            demo.pro,
            demo.explicitState && demo.orb,
            preferences,
            quota,
            RequestExit,
            RefreshSettings);
        window.Closed += Window_OnClosed;
        Application.Current.MainWindow = window;
        window.Show();

        if (demoMode)
        {
            if (demoStatus is { } status)
            {
                window.ApplyDemoStatus(status);
            }
            else
            {
                window.ApplyDemoState();
            }
            var demoExit = ParseMilliseconds(args, "--demo-exit-ms");
            if (demoExit > 0) window.ScheduleDemoExit(demoExit);
        }
        _ = StartBackgroundWorkAsync(preferences.Current.RefreshIntervalSeconds);
        return true;
    }

    public void RequestExit()
    {
        if (stopping) return;
        stopping = true;
        lifetime.Cancel();
        quota?.Cancel();
        if (window is { IsVisible: true })
        {
            window.Close();
        }
        else
        {
            Application.Current.Shutdown();
        }
    }

    public void RefreshSettings()
    {
        if (window is null) return;
        ConfigureAutoRefresh(window.Preferences.Current.RefreshIntervalSeconds);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        stopping = true;
        lifetime.Cancel();
        autoRefreshLifetime?.Cancel();
        quota?.Dispose();
        codex?.Dispose();
        singleInstance?.Dispose();
        lifetime.Dispose();
    }

    private async Task StartBackgroundWorkAsync(int refreshIntervalSeconds)
    {
        if (quota is null || codex is null) return;
        try
        {
            _ = quota.RefreshAsync(lifetime.Token);
            ConfigureAutoRefresh(refreshIntervalSeconds);

            if (LaunchMode is WpfLaunchMode.Watch or WpfLaunchMode.LaunchAndWatch)
            {
                await WatchCodexAsync(codex, launchCodexIfMissing, lifetime.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
    }

    private void ConfigureAutoRefresh(int fallbackSeconds)
    {
        autoRefreshLifetime?.Cancel();
        autoRefreshLifetime?.Dispose();
        autoRefreshLifetime = null;
        if (quota is null) return;
        autoRefreshLifetime = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        var token = autoRefreshLifetime.Token;
        _ = quota.RunAutoRefreshAsync(
            () => TimeSpan.FromSeconds(PreferenceStore.NearestRefreshInterval(window?.Preferences.Current.RefreshIntervalSeconds ?? fallbackSeconds)),
            token);
    }

    private async Task WatchCodexAsync(ICodexPresenceSource source, bool launchIfMissing, CancellationToken cancellationToken)
    {
        var absence = new CodexAbsenceConfirmation(RequiredConsecutiveAbsences);
        var initial = await source.AttachAndSampleAsync(launchIfMissing, cancellationToken).ConfigureAwait(false);
        while (!cancellationToken.IsCancellationRequested)
        {
            var observation = initial;
            initial = new(CodexObservationStatus.Unknown, 0, 0);
            if (absence.Observe(observation))
            {
                _ = dispatcher.BeginInvoke(new Action(RequestExit), DispatcherPriority.ApplicationIdle);
                return;
            }

            try
            {
                await Task.Delay(PresenceInterval, cancellationToken).ConfigureAwait(false);
                initial = await source.SampleAsync(launchIfMissing, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return; }
        }
    }

    private void ActivateExistingInstance()
    {
        dispatcher.BeginInvoke(new Action(() =>
        {
            if (window is not { IsVisible: true }) return;
            if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal;
            window.Activate();
            window.ReapplyCurrentState();
        }), DispatcherPriority.Input);
    }

    private void Window_OnClosed(object? sender, EventArgs e)
    {
        if (!stopping) RequestExit();
        Dispose();
        Application.Current.Shutdown();
    }

    private static int ParseMilliseconds(string[] args, string option)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], option, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(args[i + 1], out var value)
                && value > 0)
                return Math.Min(value, 30_000);
        }
        return 0;
    }

    private static (bool pro, bool orb, bool explicitState) ParseDemoState(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (!string.Equals(args[i], "--demo-state", StringComparison.OrdinalIgnoreCase)) continue;
            return args[i + 1].ToLowerInvariant() switch
            {
                "pro" => (true, false, true),
                "orb-plus" => (false, true, true),
                "orb-pro" => (true, true, true),
                _ => (false, false, true)
            };
        }
        return (false, false, false);
    }

    private static QuotaUiStatus? ParseDemoStatus(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (!string.Equals(args[i], "--demo-status", StringComparison.OrdinalIgnoreCase)) continue;
            return args[i + 1].ToLowerInvariant() switch
            {
                "loading" => QuotaUiStatus.Loading,
                "signedout" => QuotaUiStatus.SignedOut,
                "offline" => QuotaUiStatus.Offline,
                "ratelimited" => QuotaUiStatus.RateLimited,
                "malformed" => QuotaUiStatus.Malformed,
                "sessionchanged" => QuotaUiStatus.SessionChanged,
                "partial" => QuotaUiStatus.Partial,
                "stale" => QuotaUiStatus.Stale,
                "cancelled" => QuotaUiStatus.Cancelled,
                _ => null
            };
        }
        return null;
    }

    private static string? ParseDemoLanguage(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (!string.Equals(args[i], "--demo-language", StringComparison.OrdinalIgnoreCase)) continue;
            return args[i + 1].ToLowerInvariant() switch
            {
                "en" or "en-us" => "en-US",
                "zh" or "zh-hans" => "zh-Hans",
                _ => null
            };
        }
        return null;
    }

}
