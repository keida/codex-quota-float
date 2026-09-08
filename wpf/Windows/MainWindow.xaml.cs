using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using QuotaFloat;
using QuotaFloat.Wpf.Interaction;
using QuotaFloat.Wpf.Platform;
using QuotaFloat.Wpf.Resources;
using QuotaFloat.Wpf.Services;

namespace QuotaFloat.Wpf.Windows;

public partial class MainWindow : Window
{
    private enum OrbActivationReason
    {
        None,
        UserSelected,
        EdgeDerived,
        ScaleForced
    }

    private static readonly TimeSpan HoverDelay = TimeSpan.FromMilliseconds(300);

    private DispatcherTimer? demoExitTimer;
    private DispatcherTimer? topmostToggleTimer;
    private readonly DispatcherTimer hoverTimer;
    private bool proLayout;
    private WidgetInteractionState interactionState;
    private SavedOrbPosition? savedOrbPosition;
    private SavedOrbPosition? temporaryOrbPosition;
    private bool transitionInProgress;
    private bool releaseHandled;
    private OrbActivationReason orbActivationReason;
    private HwndSource? hwndSource;
    private SettingsWindow? settingsWindow;
    private TrayIconService? trayIconService;
    private readonly PreferenceStore preferenceStore;
    private readonly QuotaRefreshCoordinator? quotaCoordinator;
    private readonly BillingLauncher billingLauncher;
    private readonly Action? exitAction;
    private readonly Action? refreshSettingsAction;
    private QuotaDisplayState quotaState = QuotaDisplayState.LoadingState;
    private ProductScaleLayout productScaleLayout = ProductScaleLayout.Full;

    private const int WmDisplayChange = 0x007E;
    private const int WmSettingChange = 0x001A;
    private const int WmDpiChanged = 0x02E0;
    private const int WmNcHitTest = 0x0084;
    private const int HtTransparent = -1;
    private const int GwlExStyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExLayered = 0x00080000;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpFrameChanged = 0x0020;

    public MainWindow(bool proDemoState = false, bool orbDemoState = false, bool productTopmost = true, PreferenceStore? preferenceStore = null,
        QuotaRefreshCoordinator? quotaCoordinator = null, BillingLauncher? billingLauncher = null, Action? exitAction = null, Action? refreshSettingsAction = null)
    {
        InitializeComponent();
        this.preferenceStore = preferenceStore ?? PreferenceStore.Load();
        proLayout = proDemoState;
        this.quotaCoordinator = quotaCoordinator;
        this.billingLauncher = billingLauncher ?? new BillingLauncher(_ => false);
        this.exitAction = exitAction;
        this.refreshSettingsAction = refreshSettingsAction;
        SetProductTopmost(productTopmost);
        hoverTimer = new DispatcherTimer { Interval = HoverDelay };
        hoverTimer.Tick += HoverTimer_OnTick;
        SourceInitialized += MainWindow_OnSourceInitialized;
        Loaded += MainWindow_OnLoaded;

        if (orbDemoState)
        {
            if (proDemoState)
            {
                ApplyProLayout(true);
            }

            ApplyOrbDemoState(proDemoState);
        }
        else if (proDemoState)
        {
            ApplyProLayout(true);
        }

        interactionState = orbDemoState
            ? WidgetInteractionState.Orb
            : WidgetInteractionState.ManualFull;
        orbActivationReason = orbDemoState
            ? OrbActivationReason.UserSelected
            : OrbActivationReason.None;

        ApplyProductScale();
        ApplyLanguage();
        if (quotaCoordinator is not null)
        {
            quotaState = quotaCoordinator.State;
            quotaCoordinator.StateChanged += QuotaCoordinator_OnStateChanged;
            ApplyQuotaState(quotaState);
        }
    }

    private void ApplyOrbDemoState(bool proDemoState)
    {
        proLayout = proDemoState;
        Width = 74;
        Height = proDemoState ? 62 : 84;
        MinWidth = 74;
        MinHeight = Height;

        FullSurfaceGrid.Visibility = Visibility.Collapsed;
        OrbSurfaceGrid.Visibility = Visibility.Visible;
        ShellBorder.CornerRadius = new CornerRadius(8, 2, 8, 2);
        OrbPlusSurface.Visibility = proDemoState ? Visibility.Collapsed : Visibility.Visible;
        OrbProSurface.Visibility = proDemoState ? Visibility.Visible : Visibility.Collapsed;
        OrbSurfaceGrid.SetValue(AutomationProperties.AutomationIdProperty, proDemoState ? "OrbProSurface" : "OrbPlusSurface");
    }

    private void MainWindow_OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        hwndSource = HwndSource.FromHwnd(hwnd);
        hwndSource?.AddHook(MainWindowWndProc);
        SetClickThrough(false);
        trayIconService = new TrayIconService(this, ShowSettings, DisableClickThrough, ExitApplication);
        if (interactionState == WidgetInteractionState.Orb)
        {
            ApplyOrbWindowRegion();
        }
        else
        {
            ApplyFullWindowRegion();
        }
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyProductScale();
        if (interactionState == WidgetInteractionState.Orb)
        {
            SaveCurrentOrbPosition();
        }
    }

    public bool ProductTopmostEnabled { get; private set; }

    public bool ClickThroughEnabled { get; private set; }

    public PreferenceStore Preferences => preferenceStore;

    internal ProductScaleLayout ProductScaleLayout => productScaleLayout;

    public void ApplyProductScale()
    {
        var normalizedPercent = ProductScaleRules.NormalizePercent(preferenceStore.Current.FullSizePercent);
        preferenceStore.Current.FullSizePercent = normalizedPercent;
        var nextLayout = ProductScaleRules.ForPercent(normalizedPercent);
        productScaleLayout = nextLayout;

        if (interactionState == WidgetInteractionState.Orb)
        {
            if (nextLayout != ProductScaleLayout.OrbFirst
                && orbActivationReason == OrbActivationReason.ScaleForced)
            {
                RestoreScaleForcedOrb();
            }

            return;
        }

        if (nextLayout == ProductScaleLayout.OrbFirst)
        {
            if (IsLoaded)
            {
                SwitchToOrbAtCurrentLocation();
            }

            return;
        }

        ApplyFullLayoutGeometry();
        if (interactionState == WidgetInteractionState.TemporaryFull)
        {
            RepositionTemporaryFull();
        }
    }

    public void ApplyLanguage()
    {
        var english = string.Equals(preferenceStore.Current.Language, "en-US", StringComparison.OrdinalIgnoreCase);
        var text = WidgetText.For(preferenceStore.Current.Language);
        FullFiveHourLabelText.Text = text.FullFiveHourLabel;
        FullFiveHourRemainingText.Text = text.NoValue;
        FullWeeklyLabelText.Text = text.WeeklyLabel;
        FullWeeklyRemainingText.Text = text.NoValue;
        ResetCreditsText.Text = text.ResetCredits.Replace("2", text.NoValue, StringComparison.Ordinal);
        EarliestExpiryText.Text = text.EarliestExpiry.Replace(text == WidgetText.For("zh-Hans") ? "9月16日" : "Sep 16", text.NoValue, StringComparison.Ordinal);
        BillingText.Text = text.Billing;
        FullSyncText.Text = text.Synced;
        RefreshButton.Content = text.Refresh;
        RefreshButton.ToolTip = text.Refresh;
        SettingsButton.Content = text.Settings;
        SettingsButton.ToolTip = text.Settings;
        OrbPlusFiveHourLabelText.Text = text.OrbFiveHourLabel;
        OrbPlusWeeklyLabelText.Text = text.OrbWeeklyLabel;
        OrbPlusSyncText.Text = text.Synced;
        OrbProWeeklyLabelText.Text = text.OrbWeeklyLabel;
        OrbProSyncText.Text = text.Synced;

        AutomationProperties.SetName(this, "Quota Float");
        AutomationProperties.SetName(BillingText, text.BillingAutomationName);
        AutomationProperties.SetName(FullSyncText, text.SyncAutomationName);
        AutomationProperties.SetName(OrbPlusSyncText, text.SyncAutomationName);
        AutomationProperties.SetName(OrbProSyncText, text.SyncAutomationName);
        AutomationProperties.SetName(RefreshButton, text.RefreshAutomationName);
        AutomationProperties.SetName(SettingsButton, text.SettingsAutomationName);

        var textFont = new System.Windows.Media.FontFamily(english ? "Segoe UI" : "Microsoft YaHei UI, Segoe UI");
        foreach (var element in new System.Windows.Controls.TextBlock[]
        {
            FullFiveHourLabelText, FullFiveHourRemainingText, FullWeeklyLabelText, FullWeeklyRemainingText,
            ResetCreditsText, EarliestExpiryText, BillingText, FullSyncText,
            OrbPlusFiveHourLabelText, OrbPlusWeeklyLabelText, OrbPlusSyncText,
            OrbProWeeklyLabelText, OrbProSyncText
        })
        {
            element.FontFamily = textFont;
        }

        RefreshButton.FontFamily = textFont;
        SettingsButton.FontFamily = textFont;
        ProductTitleText.FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
        var compactButtonPadding = english ? new Thickness(0) : new Thickness(7, 0, 7, 0);
        RefreshButton.Padding = compactButtonPadding;
        SettingsButton.Padding = compactButtonPadding;
        UpdateLayout();
        ApplyQuotaState(quotaState);
    }

    public void ApplyQuotaState(QuotaDisplayState next)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(new Action(() => ApplyQuotaState(next)), DispatcherPriority.DataBind);
            return;
        }

        quotaState = next;
        var text = WidgetText.For(preferenceStore.Current.Language);
        if (next.Snapshot is { } snapshot)
        {
            var usePro = snapshot.Short is null && snapshot.Weekly is not null;
            if (snapshot.Short is not null || snapshot.Weekly is not null)
            {
                ApplyProLayout(usePro);
            }

            FiveHourValue.Text = snapshot.Short is { } shortWindow ? Percent(shortWindow.Remaining, text) : text.NoValue;
            WeeklyValue.Text = snapshot.Weekly is { } weeklyWindow ? Percent(weeklyWindow.Remaining, text) : text.NoValue;
            FullFiveHourRemainingText.Text = snapshot.Short is { } shortValue ? text.Remaining(shortValue, false) : text.NoValue;
            FullWeeklyRemainingText.Text = snapshot.Weekly is { } weeklyValue ? text.Remaining(weeklyValue, false) : text.NoValue;
            OrbPlusFiveHourValue.Text = FiveHourValue.Text;
            OrbPlusWeeklyValue.Text = WeeklyValue.Text;
            OrbProWeeklyValue.Text = WeeklyValue.Text;
            SetMeter(PlusFiveHourMeter, snapshot.Short?.Remaining);
            SetMeter(OrbPlusFiveHourMeter, snapshot.Short?.Remaining);
            SetMeter(WeeklyMeter, snapshot.Weekly?.Remaining);
            SetMeter(OrbPlusWeeklyMeter, snapshot.Weekly?.Remaining);
            SetMeter(OrbProWeeklyMeter, snapshot.Weekly?.Remaining);
            ResetCreditsText.Text = snapshot.Credits is { } credits ? text.ResetCredits.Replace("2", credits.ToString(), StringComparison.Ordinal) : text.ResetCredits.Replace("2", text.NoValue, StringComparison.Ordinal);
            EarliestExpiryText.Text = FormatExpiry(snapshot, text);
        }
        else
        {
            FiveHourValue.Text = text.NoValue;
            WeeklyValue.Text = text.NoValue;
            OrbPlusFiveHourValue.Text = text.NoValue;
            OrbPlusWeeklyValue.Text = text.NoValue;
            OrbProWeeklyValue.Text = text.NoValue;
            FullFiveHourRemainingText.Text = text.NoValue;
            FullWeeklyRemainingText.Text = text.NoValue;
            ResetCreditsText.Text = text.ResetCredits.Replace("2", text.NoValue, StringComparison.Ordinal);
            EarliestExpiryText.Text = text.EarliestExpiry.Replace(text == WidgetText.For("zh-Hans") ? "9月16日" : "Sep 16", text.NoValue, StringComparison.Ordinal);
            SetMeter(PlusFiveHourMeter, null);
            SetMeter(OrbPlusFiveHourMeter, null);
            SetMeter(WeeklyMeter, null);
            SetMeter(OrbPlusWeeklyMeter, null);
            SetMeter(OrbProWeeklyMeter, null);
        }

        var syncText = text.Status(next.Status, next.Snapshot is not null && next.IsRefreshing);
        FullSyncText.Text = syncText;
        OrbPlusSyncText.Text = syncText;
        OrbProSyncText.Text = syncText;
        AutomationProperties.SetName(FullSyncText, syncText);
        AutomationProperties.SetName(OrbPlusSyncText, syncText);
        AutomationProperties.SetName(OrbProSyncText, syncText);
        FullSyncText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(next.Status is QuotaUiStatus.Fresh or QuotaUiStatus.Partial ? "#35BCA4" : "#E9B63E"));
        OrbPlusSyncText.Foreground = FullSyncText.Foreground;
        OrbProSyncText.Foreground = FullSyncText.Foreground;
    }

    public void ApplyDemoState()
    {
        var now = DateTimeOffset.Now;
        var weekly = new QuotaWindow(38, now.AddDays(2).AddHours(23), 604800, "Weekly");
        var shortWindow = new QuotaWindow(72, now.AddHours(2).AddMinutes(17), 18000, "5h");
        var snapshot = proLayout
            ? new QuotaSnapshot("Pro", null, weekly, 2, new[] { now.AddDays(9) }, now, new[] { weekly })
            : new QuotaSnapshot("Plus", shortWindow, weekly, 2, new[] { now.AddDays(9) }, now, new[] { shortWindow, weekly });
        ApplyQuotaState(new QuotaDisplayState(QuotaUiStatus.Fresh, snapshot, null, now, null, false, 0, 0));
    }

    public void ApplyDemoStatus(QuotaUiStatus status)
    {
        var now = DateTimeOffset.Now;
        var weekly = new QuotaWindow(38, now.AddDays(2).AddHours(23), 604800, "Weekly");
        var shortWindow = new QuotaWindow(72, now.AddHours(2).AddMinutes(17), 18000, "5h");
        var snapshot = status switch
        {
            QuotaUiStatus.Partial => new QuotaSnapshot("Plus", shortWindow, weekly, null, Array.Empty<DateTimeOffset>(), now, new[] { shortWindow, weekly }),
            QuotaUiStatus.Stale => new QuotaSnapshot("Plus", shortWindow, weekly, 2, new[] { now.AddDays(9) }, now.AddMinutes(-5), new[] { shortWindow, weekly }),
            _ => null
        };
        ApplyQuotaState(new QuotaDisplayState(status, snapshot, status == QuotaUiStatus.Stale ? QuotaUiStatus.Offline : null,
            snapshot?.UpdatedAt, status == QuotaUiStatus.RateLimited ? TimeSpan.FromSeconds(30) : null,
            status == QuotaUiStatus.Loading, status == QuotaUiStatus.Loading ? 1 : 0, 0));
    }

    private void QuotaCoordinator_OnStateChanged(QuotaDisplayState next) => ApplyQuotaState(next);

    private static string Percent(double remaining, WidgetText text) => double.IsFinite(remaining) ? $"{Math.Clamp(remaining, 0, 100):0}%" : text.NoValue;

    private static string FormatExpiry(QuotaSnapshot snapshot, WidgetText text)
    {
        var expiry = snapshot.EarliestFutureExpiry(DateTimeOffset.Now);
        if (expiry is null) return text.EarliestExpiry.Replace(text == WidgetText.For("zh-Hans") ? "9月16日" : "Sep 16", text.NoValue, StringComparison.Ordinal);
        return ReferenceEquals(text, WidgetText.For("en-US"))
            ? $"Earliest expiry {expiry.Value.LocalDateTime:MMM d}"
            : $"最早到期 {expiry.Value.LocalDateTime:M月d日}";
    }

    private void SetMeter(UniformGrid meter, double? remaining)
    {
        var active = remaining is { } value && double.IsFinite(value) ? (int)Math.Round(Math.Clamp(value, 0, 100) / 10d, MidpointRounding.AwayFromZero) : 0;
        for (var index = 0; index < meter.Children.Count; index++)
        {
            if (meter.Children[index] is System.Windows.Shapes.Path path)
            {
                path.Fill = (Brush)FindResource(index < active ? "CivicAccentBrush" : "CivicInactiveMeterBrush");
            }
        }
    }

    public void SetProductTopmost(bool enabled)
    {
        ProductTopmostEnabled = enabled;
        Topmost = enabled;

        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
        {
            NativeMethods.SetWindowPos(
                hwnd,
                enabled ? NativeMethods.HwndTopmost : NativeMethods.HwndNoTopmost,
                0,
                0,
                0,
                0,
                NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpNoActivate | NativeMethods.SwpShowWindow);
        }
    }

    public void SetHoverExpandEnabled(bool enabled)
    {
        if (enabled)
        {
            return;
        }

        hoverTimer.Stop();
        if (interactionState == WidgetInteractionState.HoverPending)
        {
            interactionState = WidgetInteractionState.Orb;
        }
        else if (interactionState == WidgetInteractionState.TemporaryFull)
        {
            ReturnToSavedOrb();
        }
    }

    public void SetClickThrough(bool enabled)
    {
        ClickThroughEnabled = enabled;
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var style = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
        if (enabled)
        {
            style |= WsExTransparent | WsExLayered;
        }
        else
        {
            style &= ~((long)WsExTransparent | WsExLayered);
        }

        SetWindowLongPtr(hwnd, GwlExStyle, new IntPtr(style));
        SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoZOrder | SwpFrameChanged);
    }

    public void ShowSettings()
    {
        if (settingsWindow is not null)
        {
            settingsWindow.Show();
            settingsWindow.Activate();
            return;
        }

        settingsWindow = new SettingsWindow(this, preferenceStore)
        {
            Owner = this
        };
        settingsWindow.Closed += SettingsWindow_OnClosed;
        settingsWindow.Show();
        settingsWindow.Activate();
    }

    public bool TryGetCurrentPlacement(out Rect bounds, out Rect workArea) =>
        TryGetCurrentWindowAndWorkArea(out bounds, out workArea);

    private void SettingsWindow_OnClosed(object? sender, EventArgs e)
    {
        if (ReferenceEquals(sender, settingsWindow))
        {
            settingsWindow = null;
        }
    }

    private void Settings_OnClick(object sender, RoutedEventArgs e)
    {
        ShowSettings();
    }

    private async void Refresh_OnClick(object sender, RoutedEventArgs e)
    {
        if (quotaCoordinator is null) return;
        try
        {
            await quotaCoordinator.RefreshAsync();
        }
        catch (OperationCanceledException) { }
    }

    private void Billing_OnClick(object sender, MouseButtonEventArgs e)
    {
        billingLauncher.TryOpenUsageBilling();
        e.Handled = true;
    }

    public void RefreshSettings()
    {
        refreshSettingsAction?.Invoke();
    }

    private void DisableClickThrough()
    {
        SetClickThrough(false);
        settingsWindow?.RefreshClickThroughState(false);
        if (settingsWindow is { IsVisible: true })
        {
            settingsWindow.Activate();
        }
    }

    private void ExitApplication()
    {
        if (exitAction is not null) exitAction();
        else Application.Current.Shutdown();
    }

    private IntPtr MainWindowWndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == WmNcHitTest && ClickThroughEnabled)
        {
            handled = true;
            return new IntPtr(HtTransparent);
        }

        if (message is WmDisplayChange or WmSettingChange or WmDpiChanged)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(ClampToCurrentWorkArea));
        }

        return IntPtr.Zero;
    }

    private void ClampToCurrentWorkArea()
    {
        if (!IsVisible || transitionInProgress || !WindowPlacementService.TryGetPlacement(new WindowInteropHelper(this).Handle, false, out var placement))
        {
            return;
        }

        var current = placement.Bounds;
        var clamped = current;
        SavedOrbPosition? migratedSavedOrb = null;
        switch (interactionState)
        {
            case WidgetInteractionState.Orb:
            case WidgetInteractionState.HoverPending:
                if (savedOrbPosition is { } saved)
                {
                    migratedSavedOrb = OrbFullPlacement.ReanchorSavedOrb(saved, placement.WorkArea);
                    savedOrbPosition = migratedSavedOrb;
                    clamped = migratedSavedOrb.Value.Bounds;
                }
                else
                {
                    clamped = WindowPlacementService.ClampToWorkArea(current, placement.WorkArea);
                }

                break;
            case WidgetInteractionState.TemporaryFull:
                var transientSaved = temporaryOrbPosition ?? savedOrbPosition;
                if (transientSaved is { } transient)
                {
                    migratedSavedOrb = OrbFullPlacement.ReanchorSavedOrb(transient, placement.WorkArea);
                    savedOrbPosition = migratedSavedOrb;
                    temporaryOrbPosition = migratedSavedOrb;
                    clamped = OrbFullPlacement.ReanchorTemporaryFull(
                        transient,
                        placement.WorkArea,
                        current.Size);
                }
                else
                {
                    clamped = WindowPlacementService.ClampToWorkArea(current, placement.WorkArea);
                }

                break;
            default:
                clamped = WindowPlacementService.ClampToWorkArea(current, placement.WorkArea);
                break;
        }

        if (clamped == current)
        {
            return;
        }

        transitionInProgress = true;
        try
        {
            Left = clamped.Left;
            Top = clamped.Top;
            if (interactionState == WidgetInteractionState.Orb && migratedSavedOrb is { } orb)
            {
                savedOrbPosition = orb with { Bounds = clamped };
            }
        }
        finally
        {
            transitionInProgress = false;
        }
    }

    private void ApplyOrbWindowRegion()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
        {
            throw new InvalidOperationException("Orb window handle was not initialized.");
        }

        var width = (int)Math.Round(Width);
        var height = (int)Math.Round(Height);
        var points = NativeMethods.BuildAsymmetricCornerPoints(width, height);
        var region = NativeMethods.CreatePolygonRgn(points, points.Length, NativeMethods.WindingFill);
        if (region == IntPtr.Zero)
        {
            throw new InvalidOperationException("Unable to create Orb window region.");
        }

        if (NativeMethods.SetWindowRgn(hwnd, region, true) == 0)
        {
            NativeMethods.DeleteObject(region);
            throw new InvalidOperationException("Unable to apply Orb window region.");
        }
        // SetWindowRgn transfers ownership to USER32 on success.
    }

    private void ApplyFullWindowRegion()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
        {
            throw new InvalidOperationException("Full window handle was not initialized.");
        }

        var width = (int)Math.Round(Width);
        var height = (int)Math.Round(Height);
        var region = NativeMethods.CreateRoundRectRgn(0, 0, width + 1, height + 1, 4, 4);
        if (region == IntPtr.Zero)
        {
            throw new InvalidOperationException("Unable to create Full window region.");
        }

        if (NativeMethods.SetWindowRgn(hwnd, region, true) == 0)
        {
            NativeMethods.DeleteObject(region);
            throw new InvalidOperationException("Unable to apply Full window region.");
        }
        // SetWindowRgn transfers ownership to USER32 on success.
    }

    private void ClearWindowRegion()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero && NativeMethods.SetWindowRgn(hwnd, IntPtr.Zero, true) == 0)
        {
            throw new InvalidOperationException("Unable to clear Orb window region.");
        }
    }

    private static class NativeMethods
    {
        internal const int WindingFill = 2;
        internal const uint MonitorDefaultToNearest = 2;

        [DllImport("gdi32.dll", EntryPoint = "CreatePolygonRgn", SetLastError = true)]
        internal static extern IntPtr CreatePolygonRgn(Point[] points, int pointCount, int fillMode);

        [DllImport("gdi32.dll", EntryPoint = "CreateRoundRectRgn", SetLastError = true)]
        internal static extern IntPtr CreateRoundRectRgn(int left, int top, int right, int bottom, int widthEllipse, int heightEllipse);

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject(IntPtr handle);

        [DllImport("user32.dll", EntryPoint = "SetWindowRgn", SetLastError = true)]
        internal static extern int SetWindowRgn(IntPtr hwnd, IntPtr region, [MarshalAs(UnmanagedType.Bool)] bool redraw);

        internal static readonly IntPtr HwndTopmost = new(-1);
        internal static readonly IntPtr HwndNoTopmost = new(-2);
        internal const uint SwpNoSize = 0x0001;
        internal const uint SwpNoMove = 0x0002;
        internal const uint SwpNoActivate = 0x0010;
        internal const uint SwpShowWindow = 0x0040;

        [DllImport("user32.dll", EntryPoint = "SetWindowPos", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

        [DllImport("user32.dll", EntryPoint = "GetWindowRect", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);

        [DllImport("user32.dll", EntryPoint = "MonitorFromWindow", SetLastError = true)]
        internal static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

        [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetMonitorInfoNative(IntPtr monitor, ref MonitorInfo info);

        internal static bool GetMonitorInfo(IntPtr monitor, out MonitorInfo info)
        {
            info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            return GetMonitorInfoNative(monitor, ref info);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NativeRect
        {
            internal int Left;
            internal int Top;
            internal int Right;
            internal int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MonitorInfo
        {
            internal int Size;
            internal NativeRect Monitor;
            internal NativeRect Work;
            internal uint Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Point
        {
            internal int X;
            internal int Y;

            internal Point(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        internal static Point[] BuildAsymmetricCornerPoints(int width, int height) =>
        [
            new(8, 0), new(width - 2, 0), new(width - 1, 1), new(width, 2),
            new(width, height - 8), new(width - 1, height - 6), new(width - 2, height - 4),
            new(width - 3, height - 3), new(width - 4, height - 2), new(width - 6, height - 1),
            new(width - 8, height), new(2, height), new(1, height - 1), new(0, height - 2),
            new(0, 8), new(1, 6), new(2, 4), new(3, 3), new(4, 2), new(6, 1)
        ];
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);

    [DllImport("user32.dll", EntryPoint = "SetWindowPos", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

    private void ApplyProLayout(bool isPro)
    {
        var shapeAlreadyMatches = isPro
            ? !FullSurfaceGrid.Children.Contains(FiveHourSection)
            : FullSurfaceGrid.Children.Contains(FiveHourSection);
        if (isPro == proLayout && shapeAlreadyMatches)
        {
            ApplyFullLayoutGeometry();
            return;
        }

        proLayout = isPro;
        if (isPro)
        {
            if (FullSurfaceGrid.Children.Contains(FiveHourSection)) FullSurfaceGrid.Children.Remove(FiveHourSection);
            if (FullSurfaceGrid.RowDefinitions.Count == 5) FullSurfaceGrid.RowDefinitions.RemoveAt(1);
            Grid.SetRow(WeeklySection, 1);
            Grid.SetRow(ResetRow, 2);
            Grid.SetRow(ActionBar, 3);
            FullSurfaceGrid.SetValue(AutomationProperties.AutomationIdProperty, "ProFullSurface");
            PlanLabel.Text = "PRO";
            WeeklyValue.SetValue(AutomationProperties.AutomationIdProperty, "ProWeeklyRow");
            WeeklyMeter.SetValue(AutomationProperties.AutomationIdProperty, "ProWeeklyMeter");
            WeeklySection.SetValue(AutomationProperties.AutomationIdProperty, "ProWeeklySection");
            ResetRow.SetValue(AutomationProperties.AutomationIdProperty, "ProResetRow");
            ActionBar.SetValue(AutomationProperties.AutomationIdProperty, "ProActionBar");
        }
        else
        {
            if (!FullSurfaceGrid.Children.Contains(FiveHourSection)) FullSurfaceGrid.Children.Insert(1, FiveHourSection);
            if (FullSurfaceGrid.RowDefinitions.Count == 4) FullSurfaceGrid.RowDefinitions.Insert(1, new RowDefinition());
            Grid.SetRow(FiveHourSection, 1);
            Grid.SetRow(WeeklySection, 2);
            Grid.SetRow(ResetRow, 3);
            Grid.SetRow(ActionBar, 4);
            FullSurfaceGrid.SetValue(AutomationProperties.AutomationIdProperty, "PlusFullSurface");
            PlanLabel.Text = "PLUS";
            WeeklyValue.SetValue(AutomationProperties.AutomationIdProperty, "PlusWeeklyRow");
            WeeklyMeter.SetValue(AutomationProperties.AutomationIdProperty, "PlusWeeklyMeter");
            WeeklySection.SetValue(AutomationProperties.AutomationIdProperty, "PlusWeeklySection");
            ResetRow.SetValue(AutomationProperties.AutomationIdProperty, "PlusResetRow");
            ActionBar.SetValue(AutomationProperties.AutomationIdProperty, "PlusActionBar");
        }

        ApplyFullLayoutGeometry();
    }

    private void ApplyFullLayoutGeometry()
    {
        if (interactionState == WidgetInteractionState.Orb)
        {
            return;
        }

        var compact = productScaleLayout == ProductScaleLayout.CompactFull;
        var size = ProductScaleRules.FullSize(proLayout, productScaleLayout);
        MinWidth = size.Width;
        MinHeight = size.Height;
        Width = size.Width;
        Height = size.Height;

        if (proLayout)
        {
            SetRowHeights(compact ? [26, 48, 28, 24] : [26, 62, 32, 32]);
        }
        else
        {
            SetRowHeights(compact ? [26, 49, 49, 28, 24] : [26, 62, 64, 28, 32]);
        }

        var sectionMargin = compact ? new Thickness(12, 4, 12, 4) : new Thickness(12, 7, 12, 8);
        FiveHourContentGrid.Margin = sectionMargin;
        WeeklyContentGrid.Margin = sectionMargin;
        FullFiveHourLabelText.FontSize = compact ? 10 : 11;
        FullWeeklyLabelText.FontSize = compact ? 10 : 11;
        FullFiveHourRemainingText.FontSize = 9;
        FullWeeklyRemainingText.FontSize = 9;
        FullFiveHourLabelText.FontFamily = new System.Windows.Media.FontFamily(
            string.Equals(preferenceStore.Current.Language, "en-US", StringComparison.OrdinalIgnoreCase) ? "Segoe UI" : "Microsoft YaHei UI, Segoe UI");
        FullWeeklyLabelText.FontFamily = FullFiveHourLabelText.FontFamily;

        ActionBar.ColumnDefinitions.Clear();
        ActionBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(compact ? 80 : 102) });
        ActionBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(compact ? 68 : 74) });
        ActionBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(compact ? 42 : 48) });
        ActionBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(compact ? 46 : 54) });
        RefreshButton.Height = compact ? 24 : 20;
        SettingsButton.Height = compact ? 24 : 20;
        RefreshButton.Margin = compact ? new Thickness(1, 0, 1, 0) : new Thickness(2, 4, 2, 0);
        SettingsButton.Margin = compact ? new Thickness(1, 0, 1, 0) : new Thickness(2, 4, 2, 0);
        RefreshButton.VerticalAlignment = compact ? VerticalAlignment.Center : VerticalAlignment.Top;
        SettingsButton.VerticalAlignment = compact ? VerticalAlignment.Center : VerticalAlignment.Top;
        RefreshButton.FontSize = compact ? 9 : 9;
        SettingsButton.FontSize = compact ? 9 : 9;
        BillingText.FontSize = 9;
        FullSyncText.FontSize = 9;
    }

    private void SetRowHeights(IReadOnlyList<double> heights)
    {
        for (var index = 0; index < heights.Count; index++)
        {
            FullSurfaceGrid.RowDefinitions[index].Height = new GridLength(heights[index]);
        }
    }

    private void RepositionTemporaryFull()
    {
        if (temporaryOrbPosition is not { } saved || !WindowPlacementService.TryGetPlacement(new WindowInteropHelper(this).Handle, false, out var placement))
        {
            return;
        }

        var fullBounds = OrbFullPlacement.ExpandInward(
            saved.Bounds,
            placement.WorkArea,
            ProductScaleRules.FullSize(proLayout, productScaleLayout),
            saved.Edge);
        Left = fullBounds.Left;
        Top = fullBounds.Top;
        Width = fullBounds.Width;
        Height = fullBounds.Height;
        UpdateLayout();
        ApplyFullWindowRegion();
    }

    private void RestoreScaleForcedOrb()
    {
        if (transitionInProgress)
        {
            return;
        }

        var fullSize = ProductScaleRules.FullSize(proLayout, productScaleLayout);
        var restoreBounds = new Rect(Left, Top, fullSize.Width, fullSize.Height);
        if (savedOrbPosition is { } saved)
        {
            var workArea = saved.WorkArea;
            if (WindowPlacementService.TryGetPlacement(new WindowInteropHelper(this).Handle, false, out var placement))
            {
                workArea = placement.WorkArea;
            }

            restoreBounds = OrbFullPlacement.ExpandInward(
                saved.Bounds,
                workArea,
                fullSize,
                saved.Edge);
        }

        transitionInProgress = true;
        try
        {
            hoverTimer.Stop();
            ClearWindowRegion();
            interactionState = WidgetInteractionState.ManualFull;
            orbActivationReason = OrbActivationReason.None;
            FullSurfaceGrid.Visibility = Visibility.Visible;
            OrbSurfaceGrid.Visibility = Visibility.Collapsed;
            ShellBorder.CornerRadius = new CornerRadius(2);
            ApplyFullLayoutGeometry();
            Width = restoreBounds.Width;
            Height = restoreBounds.Height;
            Left = restoreBounds.Left;
            Top = restoreBounds.Top;
            temporaryOrbPosition = null;
            UpdateLayout();
            ApplyFullWindowRegion();
        }
        finally
        {
            transitionInProgress = false;
        }
    }

    private void SwitchToOrbAtCurrentLocation()
    {
        if (transitionInProgress || interactionState == WidgetInteractionState.Orb
            || !WindowPlacementService.TryGetPlacement(new WindowInteropHelper(this).Handle, false, out var placement))
        {
            return;
        }

        var orbSize = ProductScaleRules.OrbSize(proLayout);
        var orbBounds = WindowPlacementService.ClampToWorkArea(
            new Rect(placement.Bounds.Left, placement.Bounds.Top, orbSize.Width, orbSize.Height),
            placement.WorkArea);
        var edge = OrbFullPlacement.DetermineEdge(orbBounds, placement.WorkArea);

        transitionInProgress = true;
        try
        {
            hoverTimer.Stop();
            ClearWindowRegion();
            interactionState = WidgetInteractionState.Orb;
            orbActivationReason = OrbActivationReason.ScaleForced;
            FullSurfaceGrid.Visibility = Visibility.Collapsed;
            OrbSurfaceGrid.Visibility = Visibility.Visible;
            ShellBorder.CornerRadius = new CornerRadius(8, 2, 8, 2);
            OrbPlusSurface.Visibility = proLayout ? Visibility.Collapsed : Visibility.Visible;
            OrbProSurface.Visibility = proLayout ? Visibility.Visible : Visibility.Collapsed;
            OrbSurfaceGrid.SetValue(AutomationProperties.AutomationIdProperty, proLayout ? "OrbProSurface" : "OrbPlusSurface");
            MinWidth = orbSize.Width;
            MinHeight = orbSize.Height;
            Width = orbSize.Width;
            Height = orbSize.Height;
            Left = orbBounds.Left;
            Top = orbBounds.Top;
            savedOrbPosition = new SavedOrbPosition(orbBounds, placement.WorkArea, edge);
            temporaryOrbPosition = null;
            UpdateLayout();
            ApplyOrbWindowRegion();
        }
        finally
        {
            transitionInProgress = false;
        }
    }

    public void ScheduleDemoExit(int milliseconds)
    {
        demoExitTimer?.Stop();
        demoExitTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(milliseconds) };
        demoExitTimer.Tick += DemoExitTimer_OnTick;
        demoExitTimer.Start();
    }

    public void ScheduleProductTopmostToggle(int milliseconds)
    {
        topmostToggleTimer?.Stop();
        topmostToggleTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(milliseconds) };
        topmostToggleTimer.Tick += ProductTopmostToggleTimer_OnTick;
        topmostToggleTimer.Start();
    }

    private void ProductTopmostToggleTimer_OnTick(object? sender, EventArgs e)
    {
        topmostToggleTimer?.Stop();
        topmostToggleTimer = null;
        SetProductTopmost(!ProductTopmostEnabled);
    }

    private void DemoExitTimer_OnTick(object? sender, EventArgs e)
    {
        demoExitTimer?.Stop();
        topmostToggleTimer?.Stop();
        demoExitTimer = null;
        Close();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ShellSurface_OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (!preferenceStore.Current.HoverExpandFull
            || interactionState != WidgetInteractionState.Orb
            || transitionInProgress)
        {
            return;
        }

        SaveCurrentOrbPosition();
        hoverTimer.Stop();
        interactionState = WidgetInteractionState.HoverPending;
        hoverTimer.Start();
    }

    private void ShellSurface_OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (transitionInProgress)
        {
            return;
        }

        switch (interactionState)
        {
            case WidgetInteractionState.HoverPending:
                CancelPendingHover();
                break;
            case WidgetInteractionState.TemporaryFull:
                ReturnToSavedOrb();
                break;
        }
    }

    private void HoverTimer_OnTick(object? sender, EventArgs e)
    {
        hoverTimer.Stop();
        if (interactionState != WidgetInteractionState.HoverPending || transitionInProgress)
        {
            return;
        }

        if (!IsPointerInsideWindow())
        {
            CancelPendingHover();
            return;
        }

        ExpandToTemporaryFull();
    }

    private void CancelPendingHover()
    {
        hoverTimer.Stop();
        if (interactionState == WidgetInteractionState.HoverPending)
        {
            interactionState = WidgetInteractionState.Orb;
        }
    }

    private bool IsPointerInsideWindow()
    {
        var pointer = Mouse.GetPosition(this);
        return pointer.X >= 0 && pointer.Y >= 0
            && pointer.X < ActualWidth && pointer.Y < ActualHeight;
    }

    private void ExpandToTemporaryFull()
    {
        if (transitionInProgress || interactionState != WidgetInteractionState.HoverPending)
        {
            return;
        }

        if (!WindowPlacementService.TryGetPlacement(new WindowInteropHelper(this).Handle, false, out var placement))
        {
            CancelPendingHover();
            return;
        }

        var orbBounds = placement.Bounds;
        var workArea = placement.WorkArea;

        savedOrbPosition ??= new SavedOrbPosition(
            orbBounds,
            workArea,
            OrbFullPlacement.DetermineEdge(orbBounds, workArea));

        var fullSize = ProductScaleRules.FullSize(proLayout, productScaleLayout);
        var fullBounds = OrbFullPlacement.ExpandInward(
            savedOrbPosition.Value.Bounds,
            savedOrbPosition.Value.WorkArea,
            fullSize,
            savedOrbPosition.Value.Edge);
        temporaryOrbPosition = savedOrbPosition;

        transitionInProgress = true;
        try
        {
            hoverTimer.Stop();
            interactionState = WidgetInteractionState.TemporaryFull;
            ClearWindowRegion();
            FullSurfaceGrid.Visibility = Visibility.Visible;
            OrbSurfaceGrid.Visibility = Visibility.Collapsed;
            ShellBorder.CornerRadius = new CornerRadius(2);
            Width = fullBounds.Width;
            Height = fullBounds.Height;
            Left = fullBounds.Left;
            Top = fullBounds.Top;
            UpdateLayout();
            ApplyFullWindowRegion();
        }
        finally
        {
            transitionInProgress = false;
        }
    }

    private void ReturnToSavedOrb()
    {
        if (transitionInProgress || interactionState != WidgetInteractionState.TemporaryFull)
        {
            return;
        }

        var saved = temporaryOrbPosition ?? savedOrbPosition;
        if (saved is not { } savedPosition)
        {
            interactionState = WidgetInteractionState.ManualFull;
            return;
        }

        transitionInProgress = true;
        try
        {
            hoverTimer.Stop();
            interactionState = WidgetInteractionState.Orb;
            ClearWindowRegion();
            Width = savedPosition.Bounds.Width;
            Height = savedPosition.Bounds.Height;
            Left = savedPosition.Bounds.Left;
            Top = savedPosition.Bounds.Top;
            FullSurfaceGrid.Visibility = Visibility.Collapsed;
            OrbSurfaceGrid.Visibility = Visibility.Visible;
            ShellBorder.CornerRadius = new CornerRadius(8, 2, 8, 2);
            UpdateLayout();
            ApplyOrbWindowRegion();
            savedOrbPosition = savedPosition;
            temporaryOrbPosition = null;
        }
        finally
        {
            transitionInProgress = false;
        }
    }

    private void SaveCurrentOrbPosition()
    {
        if (!WindowPlacementService.TryGetPlacement(new WindowInteropHelper(this).Handle, false, out var placement))
        {
            return;
        }

        var bounds = placement.Bounds;
        var workArea = placement.WorkArea;

        if (savedOrbPosition is { } existing
            && existing.Bounds == bounds
            && existing.WorkArea == workArea)
        {
            return;
        }

        savedOrbPosition = new SavedOrbPosition(
            bounds,
            workArea,
            OrbFullPlacement.DetermineEdge(bounds, workArea));
    }

    private bool TryGetCurrentWindowAndWorkArea(out Rect bounds, out Rect workArea)
    {
        if (!WindowPlacementService.TryGetPlacement(new WindowInteropHelper(this).Handle, false, out var placement))
        {
            bounds = Rect.Empty;
            workArea = Rect.Empty;
            return false;
        }

        bounds = placement.Bounds;
        workArea = placement.WorkArea;
        return true;
    }

    private void ShellSurface_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            releaseHandled = true;
            if (interactionState == WidgetInteractionState.HoverPending)
            {
                CancelPendingHover();
            }

            DragMove();
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                releaseHandled = false;
                HandlePointerRelease();
            }));
        }
    }

    private void ShellSurface_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (releaseHandled)
        {
            releaseHandled = false;
            return;
        }

        HandlePointerRelease();
    }

    private void HandlePointerRelease()
    {
        if (releaseHandled)
        {
            return;
        }

        if (!WindowPlacementService.TryGetPlacement(new WindowInteropHelper(this).Handle, true, out var placement))
        {
            return;
        }

        var orbSize = ProductScaleRules.OrbSize(proLayout);
        if (preferenceStore.Current.EdgeAutoOrb
            && WindowPlacementService.TryGetEdgeSnap(placement.Bounds, placement.WorkArea, placement.Dpi, orbSize, out var edge, out var orbBounds))
        {
            SnapToOrb(orbBounds, placement.WorkArea, edge);
        }
        else if (interactionState == WidgetInteractionState.Orb)
        {
            SaveCurrentOrbPosition();
        }

        releaseHandled = true;
    }

    private void SnapToOrb(Rect orbBounds, Rect workArea, OrbEdge edge)
    {
        if (transitionInProgress)
        {
            return;
        }

        transitionInProgress = true;
        try
        {
            hoverTimer.Stop();
            ClearWindowRegion();
            MinWidth = orbBounds.Width;
            MinHeight = orbBounds.Height;
            Width = orbBounds.Width;
            Height = orbBounds.Height;
            Left = orbBounds.Left;
            Top = orbBounds.Top;
            FullSurfaceGrid.Visibility = Visibility.Collapsed;
            OrbSurfaceGrid.Visibility = Visibility.Visible;
            ShellBorder.CornerRadius = new CornerRadius(8, 2, 8, 2);
            OrbPlusSurface.Visibility = proLayout ? Visibility.Collapsed : Visibility.Visible;
            OrbProSurface.Visibility = proLayout ? Visibility.Visible : Visibility.Collapsed;
            OrbSurfaceGrid.SetValue(AutomationProperties.AutomationIdProperty, proLayout ? "OrbProSurface" : "OrbPlusSurface");
            interactionState = WidgetInteractionState.Orb;
            orbActivationReason = OrbActivationReason.EdgeDerived;
            savedOrbPosition = new SavedOrbPosition(orbBounds, workArea, edge);
            temporaryOrbPosition = null;
            UpdateLayout();
            ApplyOrbWindowRegion();
        }
        finally
        {
            transitionInProgress = false;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        hoverTimer.Stop();
        demoExitTimer?.Stop();
        settingsWindow?.Close();
        trayIconService?.Dispose();
        if (quotaCoordinator is not null) quotaCoordinator.StateChanged -= QuotaCoordinator_OnStateChanged;
        hwndSource?.RemoveHook(MainWindowWndProc);
        base.OnClosed(e);
    }
}
