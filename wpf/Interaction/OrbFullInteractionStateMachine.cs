using System.Windows;
using QuotaFloat.Wpf.Windows;

namespace QuotaFloat.Wpf.Interaction;

internal enum OrbEdge
{
    Left,
    Right,
    Top,
    Bottom,
    Free
}

internal readonly record struct SavedOrbPosition(
    Rect Bounds,
    Rect WorkArea,
    OrbEdge Edge,
    IntPtr Monitor = default,
    uint Dpi = 96);

internal static class OrbEdgeMapping
{
    internal static WidgetPlacement ToPlacement(this OrbEdge edge) => edge switch
    {
        OrbEdge.Left => WidgetPlacement.Left,
        OrbEdge.Right => WidgetPlacement.Right,
        OrbEdge.Top => WidgetPlacement.Top,
        OrbEdge.Bottom => WidgetPlacement.Bottom,
        _ => WidgetPlacement.Free
    };

    internal static OrbEdge ToOrbEdge(this WidgetPlacement placement) => placement switch
    {
        WidgetPlacement.Left => OrbEdge.Left,
        WidgetPlacement.Right => OrbEdge.Right,
        WidgetPlacement.Top => OrbEdge.Top,
        WidgetPlacement.Bottom => OrbEdge.Bottom,
        _ => OrbEdge.Free
    };
}

internal static class OrbFullPlacement
{
    internal static OrbEdge DetermineEdge(Rect orbBounds, Rect workArea)
    {
        const double edgeTolerance = 2;
        var distances = new[]
        {
            (Edge: OrbEdge.Left, Distance: Math.Abs(orbBounds.Left - workArea.Left)),
            (Edge: OrbEdge.Right, Distance: Math.Abs(workArea.Right - orbBounds.Right)),
            (Edge: OrbEdge.Top, Distance: Math.Abs(orbBounds.Top - workArea.Top)),
            (Edge: OrbEdge.Bottom, Distance: Math.Abs(workArea.Bottom - orbBounds.Bottom))
        };

        var nearest = distances.OrderBy(item => item.Distance).First();
        return nearest.Distance <= edgeTolerance ? nearest.Edge : OrbEdge.Free;
    }

    internal static Rect ExpandInward(
        Rect orbBounds,
        Rect workArea,
        Size fullSize,
        OrbEdge edge)
    {
        var left = orbBounds.Left;
        var top = orbBounds.Top;

        switch (edge)
        {
            case OrbEdge.Left:
                left = orbBounds.Left;
                break;
            case OrbEdge.Right:
                left = orbBounds.Right - fullSize.Width;
                break;
            case OrbEdge.Top:
                top = orbBounds.Top;
                break;
            case OrbEdge.Bottom:
                top = orbBounds.Bottom - fullSize.Height;
                break;
        }

        left = Math.Clamp(left, workArea.Left, workArea.Right - fullSize.Width);
        top = Math.Clamp(top, workArea.Top, workArea.Bottom - fullSize.Height);
        return new Rect(left, top, fullSize.Width, fullSize.Height);
    }

    internal static Rect AnchorOrb(Rect desiredBounds, Rect workArea, OrbEdge edge)
    {
        var left = Math.Clamp(desiredBounds.Left, workArea.Left, Math.Max(workArea.Left, workArea.Right - desiredBounds.Width));
        var top = Math.Clamp(desiredBounds.Top, workArea.Top, Math.Max(workArea.Top, workArea.Bottom - desiredBounds.Height));
        switch (edge)
        {
            case OrbEdge.Left:
                left = workArea.Left;
                break;
            case OrbEdge.Right:
                left = workArea.Right - desiredBounds.Width;
                break;
            case OrbEdge.Top:
                top = workArea.Top;
                break;
            case OrbEdge.Bottom:
                top = workArea.Bottom - desiredBounds.Height;
                break;
        }

        return new Rect(left, top, desiredBounds.Width, desiredBounds.Height);
    }

    internal static SavedOrbPosition ReanchorSavedOrb(SavedOrbPosition saved, Rect workArea)
    {
        var bounds = AnchorOrb(saved.Bounds, workArea, saved.Edge);
        return saved with { Bounds = bounds, WorkArea = workArea };
    }

    internal static Rect ReanchorTemporaryFull(
        SavedOrbPosition saved,
        Rect workArea,
        Size fullSize)
    {
        var migrated = ReanchorSavedOrb(saved, workArea);
        return ExpandInward(migrated.Bounds, workArea, fullSize, migrated.Edge);
    }
}
