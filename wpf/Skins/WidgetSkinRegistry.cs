using QuotaFloat.Wpf.Skins.CivicWayfinding;

namespace QuotaFloat.Wpf.Skins;

internal static class WidgetSkinRegistry
{
    public static IWidgetSkin CreateCivicOnly() => new CivicWayfindingSkin();
}
