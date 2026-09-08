using System.Windows;
using System.Windows.Threading;
using QuotaFloat;
using QuotaFloat.Wpf.Windows;

namespace QuotaFloat.Wpf.Services;

public enum WpfLaunchMode
{
    Direct,
    Watch
}

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

    public WpfApplicationCoordinator(Dispatcher dispatcher)
    {
        this.dispatcher = dispatcher;
    }

    public MainWindow? Window => window;
    public QuotaRefreshCoordinator? Quota => quota;
    public WpfLaunchMode LaunchMode { get; private set; } = WpfLaunchMode.Direct;

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
        if (ParseDemoScale(args) is { } demoScale)
        {
            preferences.Current.FullSizePercent = ProductScaleRules.NormalizePercent(demoScale);
        }
        LaunchMode = ParseLaunchMode(args, preferences.Current.FollowCodexLifecycle);
        var demoStatus = ParseDemoStatus(args);
        var demoMode = args.Any(a => string.Equals(a, "--demo-state", StringComparison.OrdinalIgnoreCase)) || demoStatus is not null;
        quota = demoMode ? null : new QuotaRefreshCoordinator(new QuotaClientSource());
        codex = demoMode ? null : new CodexLifecyclePresenceSource();

        var demo = ParseDemoState(args);
        var useOrbPreference = !demo.explicitState && string.Equals(preferences.Current.DisplayMode, "Orb", StringComparison.OrdinalIgnoreCase);
        window = new MainWindow(
            demo.pro,
            demo.explicitState ? demo.orb : useOrbPreference,
            ParseProductTopmost(args, preferences.Current.AlwaysOnTop),
            preferences,
            quota,
            new BillingLauncher(),
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
        var topmostToggle = ParseMilliseconds(args, "--topmost-toggle-ms");
        if (topmostToggle > 0) window.ScheduleProductTopmostToggle(topmostToggle);

        _ = StartBackgroundWorkAsync(preferences.Current.AutoRefresh, preferences.Current.RefreshIntervalSeconds);
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
        ConfigureAutoRefresh(window.Preferences.Current.AutoRefresh, window.Preferences.Current.RefreshIntervalSeconds);
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

    private async Task StartBackgroundWorkAsync(bool autoRefresh, int refreshIntervalSeconds)
    {
        if (quota is null || codex is null) return;
        try
        {
            _ = quota.RefreshAsync(lifetime.Token);
            ConfigureAutoRefresh(autoRefresh, refreshIntervalSeconds);

            if (LaunchMode == WpfLaunchMode.Watch)
            {
                await WatchCodexAsync(codex, lifetime.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
    }

    private void ConfigureAutoRefresh(bool enabled, int fallbackSeconds)
    {
        autoRefreshLifetime?.Cancel();
        autoRefreshLifetime?.Dispose();
        autoRefreshLifetime = null;
        if (!enabled || quota is null) return;
        autoRefreshLifetime = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        var token = autoRefreshLifetime.Token;
        _ = quota.RunAutoRefreshAsync(
            () => TimeSpan.FromSeconds(Math.Clamp(window?.Preferences.Current.RefreshIntervalSeconds ?? fallbackSeconds, 5, 3600)),
            token);
    }

    private async Task WatchCodexAsync(ICodexPresenceSource source, CancellationToken cancellationToken)
    {
        var absence = new CodexAbsenceConfirmation(RequiredConsecutiveAbsences);
        var initial = await source.AttachAndSampleAsync(cancellationToken).ConfigureAwait(false);
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
                initial = await source.SampleAsync(cancellationToken).ConfigureAwait(false);
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
            window.Topmost = window.ProductTopmostEnabled;
            window.Topmost = false;
            window.Topmost = window.ProductTopmostEnabled;
        }), DispatcherPriority.Input);
    }

    private void Window_OnClosed(object? sender, EventArgs e)
    {
        if (!stopping) RequestExit();
        Dispose();
        Application.Current.Shutdown();
    }

    private static WpfLaunchMode ParseLaunchMode(string[] args, bool followPreference)
    {
        if (args.Any(a => string.Equals(a, "--direct", StringComparison.OrdinalIgnoreCase))) return WpfLaunchMode.Direct;
        if (args.Any(a => string.Equals(a, "--watch", StringComparison.OrdinalIgnoreCase))) return WpfLaunchMode.Watch;
        return followPreference ? WpfLaunchMode.Watch : WpfLaunchMode.Direct;
    }

    private static int ParseMilliseconds(string[] args, string option)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], option, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(args[i + 1], out var value)
                && value > 0)
            {
                return Math.Min(value, 30_000);
            }
        }
        return 0;
    }

    private static bool ParseProductTopmost(string[] args, bool defaultValue)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (!string.Equals(args[i], "--topmost", StringComparison.OrdinalIgnoreCase)) continue;
            if (args[i + 1] is "off" or "false" or "0") return false;
            if (args[i + 1] is "on" or "true" or "1") return true;
        }
        return defaultValue;
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

    private static int? ParseDemoScale(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (!string.Equals(args[i], "--demo-scale", StringComparison.OrdinalIgnoreCase)
                || !int.TryParse(args[i + 1], out var value))
            {
                continue;
            }

            return ProductScaleRules.NormalizePercent(value);
        }

        return null;
    }
}
