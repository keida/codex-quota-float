using System.Windows;

namespace QuotaFloat.Wpf.Skins.CivicWayfinding;

public sealed class CivicWayfindingSkin : IWidgetSkin
{
    public string Key => "civic-wayfinding";

    public ResourceDictionary Resources => new()
    {
        Source = new Uri("/QuotaFloat.Wpf;component/Resources/CivicWayfinding.xaml", UriKind.Relative)
    };
}
