using System.Windows;

namespace QuotaFloat.Wpf.Skins;

public interface IWidgetSkin
{
    string Key { get; }

    ResourceDictionary Resources { get; }
}
