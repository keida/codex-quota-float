using System.Text.Json;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;
using System.Windows.Interop;
using QuotaFloat.Wpf.Interaction;
using QuotaFloat.Wpf.Platform;
using QuotaFloat.Wpf.Windows;

namespace QuotaFloat.Wpf.Diagnostics;

internal static class EdgeDragDiagnostics
{
    private static readonly object Gate = new();
    private static int sequence;

    internal static bool Enabled => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("QF_EDGE_DRAG_LOG"));

    internal static void LogMouseDown(Window window, MouseButtonEventArgs args, DependencyObject source, bool allowed, string reason)
    {
        if (!Enabled) return;
        var client = args.GetPosition(window);
        var screen = window.PointToScreen(client);
        Write("MouseDown", new
        {
            HitElementType = source.GetType().FullName,
            AutomationId = AutomationProperties.GetAutomationId(source),
            Name = AutomationProperties.GetName(source),
            Allowed = allowed,
            Reason = reason,
            ClientX = client.X,
            ClientY = client.Y,
            ScreenX = screen.X,
            ScreenY = screen.Y,
            Window = ReadBounds(window)
        });
    }

    internal static void LogDragStart(Window window, WidgetWindowState state)
    {
        if (!Enabled) return;
        Write("DragStart", new { DragMoveCalled = true, State = state, Window = ReadBounds(window) });
    }

    internal static void LogDragReturn(Window window, WidgetWindowState state, bool placementAvailable, WindowPlacement? placement)
    {
        if (!Enabled) return;
        Write("DragMoveReturn", new
        {
            DragMoveReturned = true,
            MouseUp = "Unavailable: DragMove consumes the modal mouse interaction; completion is posted after return.",
            State = state,
            FrozenPlacementAvailable = placementAvailable,
            FrozenMonitor = placement?.Monitor.ToInt64(),
            FrozenWorkArea = placement?.WorkArea,
            Window = ReadBounds(window)
        });
    }

    internal static void LogCompletionScheduled(WidgetWindowState state)
    {
        if (!Enabled) return;
        Write("CompletionScheduled", new { DispatcherPriority = "ApplicationIdle", State = state });
    }

    internal static void LogWindowMessage(Window window, int message, IntPtr wParam, IntPtr lParam)
    {
        if (!Enabled) return;
        Write("WindowMessage", new
        {
            Message = message == 0x0003 ? "WM_MOVE" : "WM_SIZE",
            WParam = wParam.ToInt64(),
            LParam = lParam.ToInt64(),
            Window = ReadBounds(window)
        });
    }

    internal static void LogPlacement(Window window, WidgetWindowState state, bool available, WindowPlacement? placement,
        bool edgeMatched, OrbEdge edge, Size orbSize, string monitorSelection)
    {
        if (!Enabled) return;
        Write("EdgeDetection", new
        {
            PlacementAvailable = available,
            MonitorSelection = monitorSelection,
            State = state,
            Bounds = placement?.Bounds,
            WorkArea = placement?.WorkArea,
            Dpi = placement?.Dpi,
            Threshold = placement is { } current ? WindowPlacementService.EdgeSnapLogicalPixels * current.Dpi / 96d : 0,
            EdgeMatched = edgeMatched,
            MatchedEdge = edge.ToString(),
            OrbWidth = orbSize.Width,
            OrbHeight = orbSize.Height,
            Window = ReadBounds(window)
        });
    }

    internal static void LogApplyPlacement(Window window, WidgetWindowState state, bool available,
        WindowPlacement placement, bool frozen)
    {
        if (!Enabled) return;
        Write("ApplyPlacement", new
        {
            State = state,
            PlacementAvailable = available,
            Frozen = frozen,
            Monitor = available ? placement.Monitor.ToInt64() : 0,
            WorkArea = available ? (object)placement.WorkArea : null,
            Bounds = available ? (object)placement.Bounds : null,
            Dpi = available ? placement.Dpi : 0,
            Window = ReadBounds(window)
        });
    }

    internal static void LogHover(string phase, WidgetWindowState state, SavedOrbPosition? saved, Window window)
    {
        if (!Enabled) return;
        Write(phase, new
        {
            State = state,
            SavedOrb = saved is { } current ? new
            {
                Monitor = current.Monitor.ToInt64(),
                current.Edge,
                current.Bounds,
                current.WorkArea,
                current.Dpi
            } : null,
            Window = ReadBounds(window)
        });
    }

    internal static void LogStateTransition(Window window, WidgetWindowState oldState, WidgetWindowState newState,
        bool applyStateInvoked, Visibility fullVisibility, Visibility orbVisibility)
    {
        if (!Enabled) return;
        Write("StateTransition", new
        {
            OldState = oldState,
            NewState = newState,
            ApplyStateInvoked = applyStateInvoked,
            DerivedDisplayMode = newState.DisplayMode.ToString(),
            FullVisual = fullVisibility.ToString(),
            OrbVisual = orbVisibility.ToString(),
            Window = ReadBounds(window)
        });
    }

    internal static void LogCompletionSkipped(string reason, WidgetWindowState state)
    {
        if (!Enabled) return;
        Write("CompletionSkipped", new { Reason = reason, State = state });
    }

    private static object ReadBounds(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero || !GetWindowRect(hwnd, out var rect))
            return new { Hwnd = hwnd.ToInt64(), Available = false };
        return new
        {
            Hwnd = hwnd.ToInt64(),
            Available = true,
            Left = rect.Left,
            Top = rect.Top,
            Width = rect.Right - rect.Left,
            Height = rect.Bottom - rect.Top
        };
    }

    private static void Write(string phase, object payload)
    {
        var path = Environment.GetEnvironmentVariable("QF_EDGE_DRAG_LOG");
        if (string.IsNullOrWhiteSpace(path)) return;
        lock (Gate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var entry = new
            {
                Sequence = ++sequence,
                TimestampUtc = DateTime.UtcNow.ToString("O"),
                Phase = phase,
                Payload = payload
            };
            File.AppendAllText(path, JsonSerializer.Serialize(entry) + Environment.NewLine);
        }
    }

    [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);
}
