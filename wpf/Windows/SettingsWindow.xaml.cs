using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;
using QuotaFloat.Wpf.Platform;
using QuotaFloat.Wpf.Services;

namespace QuotaFloat.Wpf.Windows;

public partial class SettingsWindow : Window
{
    private readonly MainWindow owner;
    private readonly PreferenceStore preferenceStore;
    private bool updatingControls;
    private bool appearanceRefreshPending;
    private HwndSource? hwndSource;

    private const int WmDpiChanged = 0x02E0;

    public SettingsWindow(MainWindow owner, PreferenceStore preferenceStore)
    {
        this.owner = owner;
        this.preferenceStore = preferenceStore;
        InitializeComponent();
        Background = new SolidColorBrush(Color.FromRgb(0x10, 0x1C, 0x31));
        SettingsContentGrid.Background = Background;
        SourceInitialized += SettingsWindow_OnSourceInitialized;
        Loaded += SettingsWindow_OnLoaded;
        SizeChanged += SettingsWindow_OnSizeChanged;
        Closing += SettingsWindow_OnClosing;
    }

    private UserPreferences Preferences => preferenceStore.Current;

    private void SettingsWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        PositionInActiveWorkArea();
        LoadControlsFromPreferences();
        RequestWindowAppearanceRefresh();
    }

    private void SettingsWindow_OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        hwndSource = HwndSource.FromHwnd(hwnd);
        hwndSource?.AddHook(SettingsWindowWndProc);
        RequestWindowAppearanceRefresh();
    }

    private void SettingsWindow_OnSizeChanged(object sender, SizeChangedEventArgs e) => RequestWindowAppearanceRefresh();

    private void RequestWindowAppearanceRefresh()
    {
        if (!IsLoaded || appearanceRefreshPending) return;
        appearanceRefreshPending = true;
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(() =>
        {
            appearanceRefreshPending = false;
            DwmWindowAppearance.Apply(this, WindowCornerContract.Civic);
        }));
    }

    private IntPtr SettingsWindowWndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == WmDpiChanged) RequestWindowAppearanceRefresh();
        return IntPtr.Zero;
    }

    private void SettingsWindow_OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    private void PositionInActiveWorkArea()
    {
        if (!owner.TryGetCurrentPlacement(out _, out var workArea))
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return;
        }

        Left = workArea.Left + Math.Max(0, (workArea.Width - Width) / 2);
        Top = workArea.Top + Math.Max(0, (workArea.Height - Height) / 2);
    }

    private void LoadControlsFromPreferences()
    {
        updatingControls = true;
        try
        {
            ChineseLanguageButton.IsChecked = Preferences.Language == "zh-Hans";
            EnglishLanguageButton.IsChecked = Preferences.Language == "en-US";
            Refresh15Button.IsChecked = Preferences.RefreshIntervalSeconds == 15;
            Refresh30Button.IsChecked = Preferences.RefreshIntervalSeconds == 30;
            Refresh60Button.IsChecked = Preferences.RefreshIntervalSeconds == 60;
            Refresh2mButton.IsChecked = Preferences.RefreshIntervalSeconds == 120;
            Refresh5mButton.IsChecked = Preferences.RefreshIntervalSeconds == 300;
            ApplyLanguage();
        }
        finally
        {
            updatingControls = false;
        }
    }

    private void ChineseLanguage_OnClick(object sender, RoutedEventArgs e)
    {
        if (updatingControls) return;
        Preferences.Language = "zh-Hans";
        EnglishLanguageButton.IsChecked = false;
        ApplyLanguage();
        owner.ApplyLanguage();
        SaveAndUpdateIndicator();
    }

    private void EnglishLanguage_OnClick(object sender, RoutedEventArgs e)
    {
        if (updatingControls) return;
        Preferences.Language = "en-US";
        ChineseLanguageButton.IsChecked = false;
        ApplyLanguage();
        owner.ApplyLanguage();
        SaveAndUpdateIndicator();
    }

    private void RefreshInterval_OnChecked(object sender, RoutedEventArgs e)
    {
        if (updatingControls || sender is not FrameworkElement { Tag: string value } || !int.TryParse(value, out var seconds)) return;
        Preferences.RefreshIntervalSeconds = PreferenceStore.NearestRefreshInterval(seconds);
        owner.RefreshSettings();
        SaveAndUpdateIndicator();
    }

    private void Header_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
            e.Handled = true;
        }
    }

    private void Done_OnClick(object sender, RoutedEventArgs e)
    {
        SaveAndUpdateIndicator();
        Hide();
    }

    private void SaveAndUpdateIndicator()
    {
        preferenceStore.Save();
        SavedStateText.Text = Preferences.Language == "en-US" ? "Saved" : "已保存";
        FooterSavedText.Text = Preferences.Language == "en-US" ? "Changes are stored locally" : "更改保存在本机";
    }

    private void ApplyLanguage()
    {
        var english = Preferences.Language == "en-US";
        var uiFont = new FontFamily(english ? "Segoe UI" : "Microsoft YaHei UI, Segoe UI");
        Title = english ? "Quota Float Preferences" : "Quota Float 偏好设置";
        AutomationProperties.SetName(this, english ? "Preferences" : "偏好设置");
        TitleText.Text = english ? "Preferences" : "偏好设置";
        SubtitleText.Text = english ? "Drag this title bar to move the window" : "拖动此标题栏可移动窗口";
        LanguageLabelText.Text = english ? "Language" : "语言";
        LanguageSupportText.Text = english ? "Applies immediately" : "切换后立即生效";
        RefreshIntervalLabelText.Text = english ? "Refresh interval" : "刷新间隔";
        AutoRefreshNoteText.Text = english ? "Auto refresh is always on" : "自动刷新始终开启";
        FooterSavedText.Text = english ? "Changes are stored locally" : "更改保存在本机";
        DoneButton.Content = english ? "Done" : "完成";
        ApplyLanguageFont(uiFont);
    }

    private void ApplyLanguageFont(FontFamily font)
    {
        foreach (var element in new TextBlock[] { TitleText, SubtitleText, SavedStateText, LanguageLabelText, LanguageSupportText, RefreshIntervalLabelText, AutoRefreshNoteText, FooterSavedText })
            element.FontFamily = font;
        ChineseLanguageButton.FontFamily = font;
        EnglishLanguageButton.FontFamily = font;
        Refresh15Button.FontFamily = font;
        Refresh30Button.FontFamily = font;
        Refresh60Button.FontFamily = font;
        Refresh2mButton.FontFamily = font;
        Refresh5mButton.FontFamily = font;
        DoneButton.FontFamily = font;
    }
}
