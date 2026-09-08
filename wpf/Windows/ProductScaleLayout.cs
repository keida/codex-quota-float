using System.Windows;

namespace QuotaFloat.Wpf.Windows;

internal enum ProductScaleLayout
{
    Full,
    CompactFull,
    OrbFirst
}

internal static class ProductScaleRules
{
    internal const int OrbFirstPercent = 40;
    internal const int CompactFullPercent = 70;
    internal const int FullPercent = 100;
    internal const int OrbFirstBreakpoint = 40;
    internal const int FullBreakpoint = 100;

    internal static int NormalizePercent(int percent) => percent <= 55
        ? OrbFirstPercent
        : percent <= 85
            ? CompactFullPercent
            : FullPercent;

    internal static ProductScaleLayout ForPercent(int percent) => NormalizePercent(percent) <= OrbFirstBreakpoint
        ? ProductScaleLayout.OrbFirst
        : NormalizePercent(percent) < FullBreakpoint
            ? ProductScaleLayout.CompactFull
            : ProductScaleLayout.Full;

    internal static Size FullSize(bool pro, ProductScaleLayout layout) => layout switch
    {
        ProductScaleLayout.CompactFull or ProductScaleLayout.OrbFirst => pro ? new Size(236, 128) : new Size(236, 178),
        _ => pro ? new Size(278, 156) : new Size(278, 216)
    };

    internal static Size OrbSize(bool pro) => pro ? new Size(74, 62) : new Size(74, 84);
}
