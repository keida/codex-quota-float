using System.Windows;

namespace QuotaFloat.Wpf.Windows;

public enum WidgetPlacement
{
    Free,
    Left,
    Right,
    Top,
    Bottom
}

public enum TemporaryExpansion
{
    None,
    Full
}

public enum WidgetPlan
{
    Plus,
    Pro
}

public enum WidgetDisplayMode
{
    Full,
    Orb
}

public readonly record struct WidgetWindowState(
    WidgetPlacement Placement,
    TemporaryExpansion TemporaryExpansion,
    WidgetPlan Plan)
{
    public WidgetDisplayMode DisplayMode => Placement == WidgetPlacement.Free || TemporaryExpansion == Windows.TemporaryExpansion.Full
        ? WidgetDisplayMode.Full
        : WidgetDisplayMode.Orb;
}

public sealed class WidgetWindowController
{
    private readonly Action<WidgetWindowState> apply;

    public WidgetWindowController(WidgetWindowState initialState, Action<WidgetWindowState> apply)
    {
        Validate(initialState);
        this.apply = apply;
        State = initialState;
    }

    public WidgetWindowState State { get; private set; }

    public WidgetDisplayMode DisplayMode => State.DisplayMode;

    public static Size DimensionsFor(WidgetWindowState state) => state.DisplayMode == WidgetDisplayMode.Full
        ? new Size(278, state.Plan == WidgetPlan.Pro ? 156 : 216)
        : new Size(74, state.Plan == WidgetPlan.Pro ? 62 : 84);

    public void ApplyState(WidgetWindowState next)
    {
        Validate(next);
        apply(next);
        State = next;
    }

    private static void Validate(WidgetWindowState state)
    {
        if (state.Placement == WidgetPlacement.Free && state.TemporaryExpansion != TemporaryExpansion.None)
            throw new InvalidOperationException("Free placement cannot have temporary expansion.");
    }

    public void SetPlacement(WidgetPlacement placement) =>
        ApplyState(State with { Placement = placement, TemporaryExpansion = TemporaryExpansion.None });

    public void SetTemporaryExpansion(TemporaryExpansion expansion) =>
        ApplyState(State with { TemporaryExpansion = expansion });

    public void SetPlan(WidgetPlan plan) => ApplyState(State with { Plan = plan });
}
