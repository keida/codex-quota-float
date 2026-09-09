using System;
using System.Runtime.InteropServices;
using System.Windows;
using QuotaFloat.Wpf.Interaction;

namespace QuotaFloat.Wpf.Platform;

internal readonly record struct WindowPlacement(
    IntPtr Monitor,
    Rect Bounds,
    Rect WorkArea,
    uint Dpi);

internal static class WindowPlacementService
{
    internal const uint MonitorDefaultToNearest = 2;
    internal const double EdgeSnapLogicalPixels = 24;
    private const double DefaultDpi = 96;

    internal static bool TryGetPlacement(IntPtr hwnd, bool useCursorMonitor, out WindowPlacement placement)
    {
        placement = default;
        if (hwnd == IntPtr.Zero || !GetWindowRect(hwnd, out var windowRect))
        {
            return false;
        }

        var monitor = useCursorMonitor && GetCursorPos(out var cursor)
            ? MonitorFromPoint(cursor, MonitorDefaultToNearest)
            : MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero || !TryGetMonitorInfo(monitor, out var monitorInfo))
        {
            return false;
        }

        var dpi = GetDpiForWindow(hwnd);
        placement = new WindowPlacement(
            monitor,
            new Rect(windowRect.Left, windowRect.Top, windowRect.Right - windowRect.Left, windowRect.Bottom - windowRect.Top),
            new Rect(monitorInfo.Work.Left, monitorInfo.Work.Top, monitorInfo.Work.Right - monitorInfo.Work.Left, monitorInfo.Work.Bottom - monitorInfo.Work.Top),
            dpi == 0 ? 96u : dpi);
        return true;
    }

    internal static bool TryGetEdgeSnap(
        Rect bounds,
        Rect workArea,
        uint dpi,
        Size orbSize,
        out OrbEdge edge,
        out Rect orbBounds)
    {
        var threshold = EdgeSnapLogicalPixels * dpi / DefaultDpi;
        var clampedBounds = ClampToWorkArea(bounds, workArea);
        var distances = new[]
        {
            (Edge: OrbEdge.Left, Distance: Math.Abs(clampedBounds.Left - workArea.Left)),
            (Edge: OrbEdge.Right, Distance: Math.Abs(workArea.Right - clampedBounds.Right)),
            (Edge: OrbEdge.Top, Distance: Math.Abs(clampedBounds.Top - workArea.Top)),
            (Edge: OrbEdge.Bottom, Distance: Math.Abs(workArea.Bottom - clampedBounds.Bottom))
        };
        var nearest = distances[0];
        foreach (var candidate in distances[1..])
        {
            if (candidate.Distance < nearest.Distance)
            {
                nearest = candidate;
            }
        }

        if (nearest.Distance > threshold)
        {
            edge = OrbEdge.Free;
            orbBounds = Rect.Empty;
            return false;
        }

        edge = nearest.Edge;
        orbBounds = AnchorOrb(new WindowPlacement(IntPtr.Zero, bounds, workArea, dpi), edge, orbSize);
        return true;
    }

    internal static Rect AnchorOrb(WindowPlacement placement, OrbEdge edge, Size orbSize) =>
        OrbFullPlacement.AnchorOrb(
            new Rect(placement.Bounds.Location, orbSize),
            placement.WorkArea,
            edge);

    internal static WindowPlacement FromSavedOrb(SavedOrbPosition saved) =>
        new(saved.Monitor, saved.Bounds, saved.WorkArea, saved.Dpi);

    internal static Rect ClampToWorkArea(Rect bounds, Rect workArea) =>
        new(
            Math.Clamp(bounds.Left, workArea.Left, Math.Max(workArea.Left, workArea.Right - bounds.Width)),
            Math.Clamp(bounds.Top, workArea.Top, Math.Max(workArea.Top, workArea.Bottom - bounds.Height)),
            bounds.Width,
            bounds.Height);

    [DllImport("user32.dll", EntryPoint = "GetWindowRect", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);

    [DllImport("user32.dll", EntryPoint = "GetCursorPos", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll", EntryPoint = "MonitorFromWindow", SetLastError = true)]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("user32.dll", EntryPoint = "MonitorFromPoint", SetLastError = true)]
    private static extern IntPtr MonitorFromPoint(NativePoint point, uint flags);

    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfoNativeCore(IntPtr monitor, ref MonitorInfo info);

    private static bool TryGetMonitorInfo(IntPtr monitor, out MonitorInfo info)
    {
        info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        return GetMonitorInfoNativeCore(monitor, ref info);
    }

    [DllImport("user32.dll", EntryPoint = "GetDpiForWindow", SetLastError = true)]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        internal int X;
        internal int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        internal int Size;
        internal NativeRect Monitor;
        internal NativeRect Work;
        internal uint Flags;
    }
}
