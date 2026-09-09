using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using QuotaFloat;
using QuotaFloat.Wpf.Diagnostics;
using QuotaFloat.Wpf.Interaction;
using QuotaFloat.Wpf.Platform;
using QuotaFloat.Wpf.Resources;
using QuotaFloat.Wpf.Services;

namespace QuotaFloat.Wpf.Windows;

public partial class MainWindow : Window
{
    private static readonly TimeSpan HoverDelay = TimeSpan.FromMilliseconds(300);
    private readonly DispatcherTimer hoverTimer;
    private readonly PreferenceStore preferenceStore;
    private readonly QuotaRefreshCoordinator? quotaCoordinator;
    private readonly Action? exitAction;
    private readonly Action? refreshSettingsAction;
    private readonly WidgetWindowController widgetController;
    private QuotaDisplayState quotaState = QuotaDisplayState.LoadingState;
    private SavedOrbPosition? savedOrbPosition;
    private SavedOrbPosition? temporaryOrbPosition;
    private WindowPlacement? frozenPlacementContext;
    private HwndSource? hwndSource;
    private SettingsWindow? settingsWindow;
    private TrayIconService? trayIconService;
    private bool pointerHoverPending;
    private bool appearanceRefreshPending;
    private long dragGeneration;
    private DispatcherTimer? demoExitTimer;

    private const int WmDisplayChange = 0x007E;
    private const int WmSettingChange = 0x001A;
    private const int WmDpiChanged = 0x02E0;
    private const int WmMove = 0x0003;
    private const int WmSize = 0x0005;

    public MainWindow(bool proDemoState = false, bool orbDemoState = false, PreferenceStore? preferenceStore = null,
        QuotaRefreshCoordinator? quotaCoordinator = null, Action? exitAction = null, Action? refreshSettingsAction = null)
    {
        InitializeComponent();
        Background = new SolidColorBrush(Color.FromRgb(0x10, 0x1C, 0x31));
        SurfaceHostBorder.Background = Background;
        this.preferenceStore = preferenceStore ?? PreferenceStore.Load();
        this.quotaCoordinator = quotaCoordinator;
        this.exitAction = exitAction;
        this.refreshSettingsAction = refreshSettingsAction;
        widgetController = new(
            new WidgetWindowState(
                orbDemoState ? WidgetPlacement.Left : WidgetPlacement.Free,
                TemporaryExpansion.None,
                proDemoState ? WidgetPlan.Pro : WidgetPlan.Plus),
            ApplyState);

        hoverTimer = new DispatcherTimer { Interval = HoverDelay };
        hoverTimer.Tick += HoverTimer_OnTick;
        SourceInitialized += MainWindow_OnSourceInitialized;
        Loaded += MainWindow_OnLoaded;
        ApplyState(widgetController.State);
        ApplyLanguage();
        if (quotaCoordinator is not null)
        {
            quotaState = quotaCoordinator.State;
            quotaCoordinator.StateChanged += QuotaCoordinator_OnStateChanged;
            ApplyQuotaState(quotaState);
        }
    }

    public bool ProductTopmostEnabled => true;
    public PreferenceStore Preferences => preferenceStore;
    internal WidgetWindowState ControllerState => widgetController.State;
    internal WidgetDisplayMode DisplayMode => widgetController.DisplayMode;

    private void MainWindow_OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        hwndSource = HwndSource.FromHwnd(hwnd);
        hwndSource?.AddHook(MainWindowWndProc);
        trayIconService = new TrayIconService(this, ShowSettings, ExitApplication);
        widgetController.ApplyState(widgetController.State);
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (widgetController.State.Placement != WidgetPlacement.Free)
            SaveCurrentOrbPosition();
        ApplyState(widgetController.State);
    }

    private void ApplyState(WidgetWindowState state)
    {
        var isPro = state.Plan == WidgetPlan.Pro;
        ApplyPlanLayout(isPro);
        var display = state.DisplayMode;
        var targetSize = WidgetWindowController.DimensionsFor(state);
        FullSurfaceGrid.Visibility = display == WidgetDisplayMode.Full ? Visibility.Visible : Visibility.Collapsed;
        OrbSurfaceGrid.Visibility = display == WidgetDisplayMode.Orb ? Visibility.Visible : Visibility.Collapsed;
        MinWidth = targetSize.Width;
        MinHeight = targetSize.Height;
        Width = targetSize.Width;
        Height = targetSize.Height;
        Topmost = true;

        var placementAvailable = false;
        var placement = default(WindowPlacement);
        var placementFrozen = false;
        if (frozenPlacementContext is { } frozen)
        {
            placement = frozen;
            placementAvailable = true;
            placementFrozen = true;
        }
        else if (state.Placement != WidgetPlacement.Free && savedOrbPosition is { } saved)
        {
            placement = WindowPlacementService.FromSavedOrb(saved);
            placementAvailable = true;
            placementFrozen = true;
        }
        else if (IsLoaded)
        {
            placementAvailable = WindowPlacementService.TryGetPlacement(new WindowInteropHelper(this).Handle, false, out placement);
        }
        EdgeDragDiagnostics.LogApplyPlacement(this, state, placementAvailable, placement, placementFrozen);
        if (placementAvailable)
        {
            if (state.Placement == WidgetPlacement.Free)
            {
                var clamped = WindowPlacementService.ClampToWorkArea(placement.Bounds, placement.WorkArea);
                Left = clamped.Left;
                Top = clamped.Top;
            }
            else
            {
                var edge = state.Placement.ToOrbEdge();
                var orbSize = isPro ? new Size(74, 62) : new Size(74, 84);
                var storedOrb = savedOrbPosition?.Bounds ?? placement.Bounds;
                var orbBounds = WindowPlacementService.AnchorOrb(placement, edge, orbSize);
                savedOrbPosition = new SavedOrbPosition(orbBounds, placement.WorkArea, edge, placement.Monitor, placement.Dpi);
                if (display == WidgetDisplayMode.Orb)
                {
                    Left = orbBounds.Left;
                    Top = orbBounds.Top;
                }
                else if (state.TemporaryExpansion == TemporaryExpansion.Full)
                {
                    temporaryOrbPosition = savedOrbPosition;
                    var full = OrbFullPlacement.ReanchorTemporaryFull(savedOrbPosition.Value, placement.WorkArea, new Size(Width, Height));
                    Left = full.Left;
                    Top = full.Top;
                }
            }
        }

        RequestWindowAppearanceRefresh();
    }

    private void ApplyPlanLayout(bool isPro)
    {
        PlanLabel.Text = isPro ? "PRO" : "PLUS";
        PlanLabel.SetValue(AutomationProperties.AutomationIdProperty, isPro ? "ProFullPlan" : "PlusFullPlan");
        AutomationProperties.SetName(PlanLabel, isPro ? "Pro plan" : "Plus plan");
        if (isPro)
        {
            if (FullSurfaceGrid.Children.Contains(FiveHourSection)) FullSurfaceGrid.Children.Remove(FiveHourSection);
            if (FullSurfaceGrid.RowDefinitions.Count == 5) FullSurfaceGrid.RowDefinitions.RemoveAt(1);
            Grid.SetRow(WeeklySection, 1);
            Grid.SetRow(ResetRow, 2);
            Grid.SetRow(ActionBar, 3);
            SetRowHeights([26, 62, 28, 32]);
            FullSurfaceGrid.SetValue(AutomationProperties.AutomationIdProperty, "ProFullSurface");
            WeeklyValue.SetValue(AutomationProperties.AutomationIdProperty, "ProWeeklyRow");
            WeeklyMeter.SetValue(AutomationProperties.AutomationIdProperty, "ProWeeklyMeter");
            OrbSurfaceGrid.SetValue(AutomationProperties.AutomationIdProperty, "OrbProSurface");
            OrbPlusSurface.Visibility = Visibility.Collapsed;
            OrbProSurface.Visibility = Visibility.Visible;
        }
        else
        {
            if (!FullSurfaceGrid.Children.Contains(FiveHourSection)) FullSurfaceGrid.Children.Insert(1, FiveHourSection);
            if (FullSurfaceGrid.RowDefinitions.Count == 4) FullSurfaceGrid.RowDefinitions.Insert(1, new RowDefinition());
            Grid.SetRow(FiveHourSection, 1);
            Grid.SetRow(WeeklySection, 2);
            Grid.SetRow(ResetRow, 3);
            Grid.SetRow(ActionBar, 4);
            SetRowHeights([26, 62, 64, 28, 32]);
            FullSurfaceGrid.SetValue(AutomationProperties.AutomationIdProperty, "PlusFullSurface");
            WeeklyValue.SetValue(AutomationProperties.AutomationIdProperty, "PlusWeeklyRow");
            WeeklyMeter.SetValue(AutomationProperties.AutomationIdProperty, "PlusWeeklyMeter");
            OrbSurfaceGrid.SetValue(AutomationProperties.AutomationIdProperty, "OrbPlusSurface");
            OrbPlusSurface.Visibility = Visibility.Visible;
            OrbProSurface.Visibility = Visibility.Collapsed;
        }
    }

    private void SetRowHeights(IReadOnlyList<double> heights)
    {
        for (var index = 0; index < heights.Count; index++)
            FullSurfaceGrid.RowDefinitions[index].Height = new GridLength(heights[index]);
    }

    public void ApplyLanguage()
    {
        var english = preferenceStore.Current.Language == "en-US";
        var text = WidgetText.For(preferenceStore.Current.Language);
        FullFiveHourLabelText.Text = text.FullFiveHourLabel;
        FullWeeklyLabelText.Text = text.WeeklyLabel;
        RefreshButton.Content = text.Refresh;
        RefreshButton.ToolTip = text.Refresh;
        SettingsButton.Content = text.Settings;
        SettingsButton.ToolTip = text.Settings;
        FullSyncText.Text = text.Synced;
        OrbPlusFiveHourLabelText.Text = text.OrbFiveHourLabel;
        OrbPlusWeeklyLabelText.Text = text.OrbWeeklyLabel;
        OrbPlusSyncText.Text = text.Synced;
        OrbProWeeklyLabelText.Text = text.OrbWeeklyLabel;
        OrbProSyncText.Text = text.Synced;
        AutomationProperties.SetName(this, "Quota Float");
        AutomationProperties.SetName(FullSyncText, text.SyncAutomationName);
        AutomationProperties.SetName(OrbPlusSyncText, text.SyncAutomationName);
        AutomationProperties.SetName(OrbProSyncText, text.SyncAutomationName);
        AutomationProperties.SetName(RefreshButton, text.RefreshAutomationName);
        AutomationProperties.SetName(SettingsButton, text.SettingsAutomationName);
        var font = new FontFamily(english ? "Segoe UI" : "Microsoft YaHei UI, Segoe UI");
        foreach (var element in new TextBlock[]
        {
            FullFiveHourLabelText, FullFiveHourRemainingText, FullWeeklyLabelText, FullWeeklyRemainingText,
            ResetCreditsText, EarliestExpiryText, FullSyncText, OrbPlusFiveHourLabelText,
            OrbPlusWeeklyLabelText, OrbPlusSyncText, OrbProWeeklyLabelText, OrbProSyncText
        }) element.FontFamily = font;
        RefreshButton.FontFamily = font;
        SettingsButton.FontFamily = font;
        ProductTitleText.FontFamily = new FontFamily("Segoe UI");
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
            var plan = snapshot.Short is null && snapshot.Weekly is not null ? WidgetPlan.Pro : WidgetPlan.Plus;
            if (plan != widgetController.State.Plan) widgetController.SetPlan(plan);
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

        var status = text.Status(next.Status, next.Snapshot is not null && next.IsRefreshing);
        var fullStatus = next.Status == QuotaUiStatus.Fresh && next.Snapshot is not null && !next.IsRefreshing
            ? (preferenceStore.Current.Language == "en-US" ? "Synced · now" : "已同步 · 刚刚")
            : status;
        FullSyncText.Text = fullStatus;
        OrbPlusSyncText.Text = status;
        OrbProSyncText.Text = status;
        AutomationProperties.SetName(FullSyncText, fullStatus);
        AutomationProperties.SetName(OrbPlusSyncText, status);
        AutomationProperties.SetName(OrbProSyncText, status);
        var color = next.Status is QuotaUiStatus.Fresh or QuotaUiStatus.Partial ? "#35BCA4" : "#E9B63E";
        FullSyncText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        OrbPlusSyncText.Foreground = FullSyncText.Foreground;
        OrbProSyncText.Foreground = FullSyncText.Foreground;
    }

    public void ApplyDemoState()
    {
        var now = DateTimeOffset.Now;
        var weekly = new QuotaWindow(38, now.AddDays(2).AddHours(23), 604800, "Weekly");
        var shortWindow = new QuotaWindow(72, now.AddHours(2).AddMinutes(17), 18000, "5h");
        var snapshot = widgetController.State.Plan == WidgetPlan.Pro
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
        return text == WidgetText.For("en-US") ? $"Earliest expiry {expiry.Value.LocalDateTime:MMM d}" : $"最早到期 {expiry.Value.LocalDateTime:M月d日}";
    }

    private void SetMeter(UniformGrid meter, double? remaining)
    {
        var active = remaining is { } value && double.IsFinite(value) ? (int)Math.Round(Math.Clamp(value, 0, 100) / 10d, MidpointRounding.AwayFromZero) : 0;
        for (var index = 0; index < meter.Children.Count; index++)
            if (meter.Children[index] is System.Windows.Shapes.Path path)
                path.Fill = (Brush)FindResource(index < active ? "CivicAccentBrush" : "CivicInactiveMeterBrush");
    }

    public void ShowSettings()
    {
        if (settingsWindow is not null)
        {
            settingsWindow.Show();
            settingsWindow.Activate();
            return;
        }
        settingsWindow = new SettingsWindow(this, preferenceStore) { Owner = this };
        settingsWindow.Closed += SettingsWindow_OnClosed;
        settingsWindow.Show();
        settingsWindow.Activate();
    }

    public bool TryGetCurrentPlacement(out Rect bounds, out Rect workArea)
    {
        if (WindowPlacementService.TryGetPlacement(new WindowInteropHelper(this).Handle, false, out var placement))
        {
            bounds = placement.Bounds;
            workArea = placement.WorkArea;
            return true;
        }
        bounds = Rect.Empty;
        workArea = Rect.Empty;
        return false;
    }

    public void RefreshSettings() => refreshSettingsAction?.Invoke();

    public void ScheduleDemoExit(int milliseconds)
    {
        demoExitTimer?.Stop();
        demoExitTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Math.Clamp(milliseconds, 1, 30_000)) };
        demoExitTimer.Tick += DemoExitTimer_OnTick;
        demoExitTimer.Start();
    }

    private void DemoExitTimer_OnTick(object? sender, EventArgs e)
    {
        demoExitTimer?.Stop();
        Close();
    }

    private void SettingsWindow_OnClosed(object? sender, EventArgs e)
    {
        if (ReferenceEquals(sender, settingsWindow)) settingsWindow = null;
    }

    private void Settings_OnClick(object sender, RoutedEventArgs e) => ShowSettings();

    private async void Refresh_OnClick(object sender, RoutedEventArgs e)
    {
        if (quotaCoordinator is null) return;
        try { await quotaCoordinator.RefreshAsync(); }
        catch (OperationCanceledException) { }
    }

    private void ExitApplication()
    {
        if (exitAction is not null) exitAction();
        else Application.Current.Shutdown();
    }

    private IntPtr MainWindowWndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message is WmMove or WmSize)
            EdgeDragDiagnostics.LogWindowMessage(this, message, wParam, lParam);
        if (message is WmDisplayChange or WmSettingChange or WmDpiChanged)
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => ApplyState(widgetController.State)));
        return IntPtr.Zero;
    }

    private void ShellSurface_OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (widgetController.DisplayMode != WidgetDisplayMode.Orb || pointerHoverPending) return;
        SaveCurrentOrbPosition();
        pointerHoverPending = true;
        hoverTimer.Start();
    }

    private void ShellSurface_OnMouseLeave(object sender, MouseEventArgs e)
    {
        EdgeDragDiagnostics.LogHover("HoverLeaveBefore", widgetController.State, savedOrbPosition, this);
        if (widgetController.State.TemporaryExpansion == TemporaryExpansion.Full)
            widgetController.SetTemporaryExpansion(TemporaryExpansion.None);
        pointerHoverPending = false;
        hoverTimer.Stop();
        EdgeDragDiagnostics.LogHover("HoverLeaveAfter", widgetController.State, savedOrbPosition, this);
    }

    private void HoverTimer_OnTick(object? sender, EventArgs e)
    {
        hoverTimer.Stop();
        pointerHoverPending = false;
        if (widgetController.DisplayMode != WidgetDisplayMode.Orb || !IsPointerInsideWindow()) return;
        temporaryOrbPosition = savedOrbPosition;
        EdgeDragDiagnostics.LogHover("HoverExpandBefore", widgetController.State, savedOrbPosition, this);
        widgetController.SetTemporaryExpansion(TemporaryExpansion.Full);
        EdgeDragDiagnostics.LogHover("HoverExpandAfter", widgetController.State, savedOrbPosition, this);
    }

    private bool IsPointerInsideWindow()
    {
        var pointer = Mouse.GetPosition(this);
        return pointer.X >= 0 && pointer.Y >= 0 && pointer.X <= ActualWidth && pointer.Y <= ActualHeight;
    }

    private void ShellSurface_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source) return;
        var buttonSource = FindVisualParent<Button>(source);
        if (buttonSource is not null)
        {
            EdgeDragDiagnostics.LogMouseDown(this, e, source, false, "button-descendant");
            return;
        }
        if (e.ButtonState != MouseButtonState.Pressed)
        {
            EdgeDragDiagnostics.LogMouseDown(this, e, source, false, $"button-state-{e.ButtonState}");
            return;
        }
        EdgeDragDiagnostics.LogMouseDown(this, e, source, true, "surface-drag-allowed");
        hoverTimer.Stop();
        pointerHoverPending = false;
        EdgeDragDiagnostics.LogDragStart(this, widgetController.State);
        DragMove();
        var placementAvailable = WindowPlacementService.TryGetPlacement(new WindowInteropHelper(this).Handle, true, out var frozenPlacement);
        EdgeDragDiagnostics.LogDragReturn(this, widgetController.State, placementAvailable, placementAvailable ? frozenPlacement : null);
        var completedDrag = ++dragGeneration;
        EdgeDragDiagnostics.LogCompletionScheduled(widgetController.State);
        Dispatcher.BeginInvoke(
            DispatcherPriority.ApplicationIdle,
            new Action(() =>
            {
                if (completedDrag != dragGeneration)
                {
                    EdgeDragDiagnostics.LogCompletionSkipped("drag-generation-mismatch", widgetController.State);
                    return;
                }
                if (!IsVisible)
                {
                    EdgeDragDiagnostics.LogCompletionSkipped("window-not-visible", widgetController.State);
                    return;
                }
                HandlePointerRelease(placementAvailable, frozenPlacement);
            }));
        e.Handled = true;
    }

    private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child is not null)
        {
            if (child is T match) return match;
            child = VisualTreeHelper.GetParent(child);
        }
        return null;
    }

    private void HandlePointerRelease(bool placementAvailable, WindowPlacement placement)
    {
        var orbSize = widgetController.State.Plan == WidgetPlan.Pro ? new Size(74, 62) : new Size(74, 84);
        var edgeMatched = false;
        var edge = OrbEdge.Free;
        var orbBounds = Rect.Empty;
        if (placementAvailable && widgetController.DisplayMode == WidgetDisplayMode.Full)
            edgeMatched = WindowPlacementService.TryGetEdgeSnap(placement.Bounds, placement.WorkArea, placement.Dpi, orbSize, out edge, out orbBounds);
        EdgeDragDiagnostics.LogPlacement(this, widgetController.State, placementAvailable,
            placementAvailable ? placement : null, edgeMatched, edge, orbSize,
            "frozen-from-TryGetPlacement(useCursorMonitor=true)-at-DragMoveReturn");
        if (edgeMatched)
        {
            var oldState = widgetController.State;
            savedOrbPosition = new SavedOrbPosition(orbBounds, placement.WorkArea, edge, placement.Monitor, placement.Dpi);
            temporaryOrbPosition = null;
            var nextState = oldState with { Placement = edge.ToPlacement(), TemporaryExpansion = TemporaryExpansion.None };
            frozenPlacementContext = placement;
            try
            {
                widgetController.ApplyState(nextState);
            }
            finally
            {
                frozenPlacementContext = null;
            }
            EdgeDragDiagnostics.LogStateTransition(this, oldState, nextState, true, FullSurfaceGrid.Visibility, OrbSurfaceGrid.Visibility);
        }
        else if (widgetController.State.Placement != WidgetPlacement.Free)
        {
            savedOrbPosition = null;
            temporaryOrbPosition = null;
            widgetController.SetPlacement(WidgetPlacement.Free);
        }
    }

    private void SaveCurrentOrbPosition()
    {
        if (!WindowPlacementService.TryGetPlacement(new WindowInteropHelper(this).Handle, false, out var placement)) return;
        var edge = widgetController.State.Placement.ToOrbEdge();
        if (edge == OrbEdge.Free) edge = OrbFullPlacement.DetermineEdge(placement.Bounds, placement.WorkArea);
        if (edge != OrbEdge.Free)
            savedOrbPosition = new SavedOrbPosition(placement.Bounds, placement.WorkArea, edge, placement.Monitor, placement.Dpi);
    }

    private void RequestWindowAppearanceRefresh()
    {
        if (!IsLoaded || appearanceRefreshPending) return;
        appearanceRefreshPending = true;
        Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
        {
            appearanceRefreshPending = false;
            ApplyWindowAppearance();
        }));
    }

    private void ApplyWindowAppearance()
    {
        if (!IsLoaded) return;
        DwmWindowAppearance.Apply(this, WindowCornerContract.Civic);
    }

    internal void ReapplyCurrentState() => widgetController.ApplyState(widgetController.State);

    protected override void OnClosed(EventArgs e)
    {
        hoverTimer.Stop();
        demoExitTimer?.Stop();
        if (quotaCoordinator is not null) quotaCoordinator.StateChanged -= QuotaCoordinator_OnStateChanged;
        trayIconService?.Dispose();
        settingsWindow?.Close();
        hwndSource?.RemoveHook(MainWindowWndProc);
        base.OnClosed(e);
    }

}
