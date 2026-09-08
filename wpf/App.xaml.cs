using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
using QuotaFloat.Wpf.Skins;
using QuotaFloat.Wpf.Services;

namespace QuotaFloat.Wpf;

public partial class App : Application
{
    private WpfApplicationCoordinator? coordinator;

    protected override void OnStartup(StartupEventArgs e)
    {
        RenderOptions.ProcessRenderMode = RenderMode.Default;
        base.OnStartup(e);

        var civicSkin = WidgetSkinRegistry.CreateCivicOnly();
        Resources.MergedDictionaries.Add(civicSkin.Resources);

        coordinator = new WpfApplicationCoordinator(Dispatcher);
        if (!coordinator.Start(e.Args))
        {
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        coordinator?.Dispose();
        base.OnExit(e);
    }
}
