using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Automation;
using System.Windows.Media;
using QuotaFloat.Wpf.Resources;
using QuotaFloat.Wpf.Services;

namespace QuotaFloat.Wpf.Windows;

public partial class SettingsWindow : Window
{
    private readonly MainWindow owner;
    private readonly PreferenceStore preferenceStore;
    private bool updatingControls;
    private bool closingFromDone;

    public SettingsWindow(MainWindow owner, PreferenceStore preferenceStore)
    {
        this.owner = owner;
        this.preferenceStore = preferenceStore;
        updatingControls = true;
        InitializeComponent();
        updatingControls = false;
        Loaded += SettingsWindow_OnLoaded;
        Activated += SettingsWindow_OnActivated;
        Closing += SettingsWindow_OnClosing;
    }

    private UserPreferences Preferences => preferenceStore.Current;

    public void RefreshClickThroughState(bool enabled)
    {
        updatingControls = true;
        try
        {
            ClickThroughToggle.IsChecked = enabled;
        }
        finally
        {
            updatingControls = false;
        }
    }

    private void SettingsWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        PositionInActiveWorkArea();
        LoadControlsFromPreferences();
        FocusFirstActionableControl();
    }

    private void SettingsWindow_OnActivated(object? sender, EventArgs e)
    {
        closingFromDone = false;
        if (owner.ProductTopmostEnabled)
        {
            Topmost = true;
        }
    }

    private void SettingsWindow_OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!closingFromDone)
        {
            SaveAndUpdateIndicator();
        }

        e.Cancel = true;
        Hide();
    }

    private void PositionInActiveWorkArea()
    {
        if (!owner.TryGetCurrentPlacement(out var widgetBounds, out var workArea))
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return;
        }

        Left = workArea.Left + Math.Max(0, (workArea.Width - Width) / 2);
        Top = workArea.Top + Math.Max(0, (workArea.Height - Height) / 2);
        if (widgetBounds.IntersectsWith(new Rect(Left, Top, Width, Height)))
        {
            Top = workArea.Top + Math.Max(0, (workArea.Height - Height) / 2);
        }
    }

    private void LoadControlsFromPreferences()
    {
        updatingControls = true;
        try
        {
            FullViewButton.IsChecked = Preferences.DisplayMode == "Full";
            OrbViewButton.IsChecked = Preferences.DisplayMode == "Orb";
            ChineseLanguageButton.IsChecked = Preferences.Language == "zh-Hans";
            EnglishLanguageButton.IsChecked = Preferences.Language == "en-US";
            FullSizeSlider.Value = Preferences.FullSizePercent;
            AlwaysTopToggle.IsChecked = Preferences.AlwaysOnTop;
            EdgeAutoOrbToggle.IsChecked = Preferences.EdgeAutoOrb;
            HoverExpandToggle.IsChecked = Preferences.HoverExpandFull;
            ClickThroughToggle.IsChecked = owner.ClickThroughEnabled;
            AutoRefreshToggle.IsChecked = Preferences.AutoRefresh;
            FollowLifecycleToggle.IsChecked = Preferences.FollowCodexLifecycle;
            LowQuotaToggle.IsChecked = Preferences.LowQuotaAlerts;
            RefreshIntervalInput.Text = Preferences.RefreshIntervalSeconds.ToString(CultureInfo.InvariantCulture);
            ApplyLanguage();
            UpdateFullSizeText();
        }
        finally
        {
            updatingControls = false;
        }
    }

    private void DefaultFull_OnClick(object sender, RoutedEventArgs e)
    {
        if (updatingControls)
        {
            return;
        }

        Preferences.DisplayMode = "Full";
        OrbViewButton.IsChecked = false;
        SaveAndUpdateIndicator();
    }

    private void DefaultOrb_OnClick(object sender, RoutedEventArgs e)
    {
        if (updatingControls)
        {
            return;
        }

        Preferences.DisplayMode = "Orb";
        FullViewButton.IsChecked = false;
        SaveAndUpdateIndicator();
    }

    private void ChineseLanguage_OnClick(object sender, RoutedEventArgs e)
    {
        if (updatingControls)
        {
            return;
        }

        Preferences.Language = "zh-Hans";
        EnglishLanguageButton.IsChecked = false;
        ApplyLanguage();
        owner.ApplyLanguage();
        SaveAndUpdateIndicator();
    }

    private void EnglishLanguage_OnClick(object sender, RoutedEventArgs e)
    {
        if (updatingControls)
        {
            return;
        }

        Preferences.Language = "en-US";
        ChineseLanguageButton.IsChecked = false;
        ApplyLanguage();
        owner.ApplyLanguage();
        SaveAndUpdateIndicator();
    }

    private void FullSizeSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (updatingControls)
        {
            return;
        }

        var normalized = ProductScaleRules.NormalizePercent((int)Math.Round(FullSizeSlider.Value, MidpointRounding.AwayFromZero));
        if ((int)Math.Round(FullSizeSlider.Value, MidpointRounding.AwayFromZero) != normalized)
        {
            updatingControls = true;
            try
            {
                FullSizeSlider.Value = normalized;
            }
            finally
            {
                updatingControls = false;
            }
        }

        Preferences.FullSizePercent = normalized;
        UpdateFullSizeText();
        owner.ApplyProductScale();
        SaveAndUpdateIndicator();
    }

    private void RestoreFullSize_OnClick(object sender, RoutedEventArgs e)
    {
        updatingControls = true;
        try
        {
            FullSizeSlider.Value = 100;
            Preferences.FullSizePercent = 100;
            UpdateFullSizeText();
        }
        finally
        {
            updatingControls = false;
        }

        owner.ApplyProductScale();
        SaveAndUpdateIndicator();
    }

    private void AlwaysTop_OnClick(object sender, RoutedEventArgs e)
    {
        Preferences.AlwaysOnTop = AlwaysTopToggle.IsChecked == true;
        owner.SetProductTopmost(Preferences.AlwaysOnTop);
        SaveAndUpdateIndicator();
    }

    private void EdgeAutoOrb_OnClick(object sender, RoutedEventArgs e)
    {
        Preferences.EdgeAutoOrb = EdgeAutoOrbToggle.IsChecked == true;
        SaveAndUpdateIndicator();
    }

    private void HoverExpand_OnClick(object sender, RoutedEventArgs e)
    {
        Preferences.HoverExpandFull = HoverExpandToggle.IsChecked == true;
        owner.SetHoverExpandEnabled(Preferences.HoverExpandFull);
        SaveAndUpdateIndicator();
    }

    private void ClickThrough_OnClick(object sender, RoutedEventArgs e)
    {
        owner.SetClickThrough(ClickThroughToggle.IsChecked == true);
        SaveAndUpdateIndicator();
    }

    private void AutoRefresh_OnClick(object sender, RoutedEventArgs e)
    {
        Preferences.AutoRefresh = AutoRefreshToggle.IsChecked == true;
        owner.RefreshSettings();
        SaveAndUpdateIndicator();
    }

    private void FollowLifecycle_OnClick(object sender, RoutedEventArgs e)
    {
        Preferences.FollowCodexLifecycle = FollowLifecycleToggle.IsChecked == true;
        SaveAndUpdateIndicator();
    }

    private void LowQuota_OnClick(object sender, RoutedEventArgs e)
    {
        Preferences.LowQuotaAlerts = LowQuotaToggle.IsChecked == true;
        SaveAndUpdateIndicator();
    }

    private void RefreshInterval_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (updatingControls || string.IsNullOrWhiteSpace(RefreshIntervalInput.Text))
        {
            return;
        }

        if (int.TryParse(RefreshIntervalInput.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
        {
            Preferences.RefreshIntervalSeconds = Math.Clamp(seconds, 5, 3600);
            owner.RefreshSettings();
            SaveAndUpdateIndicator();
        }
    }

    private void Help_OnClick(object sender, RoutedEventArgs e)
    {
        var message = Preferences.Language == "en-US"
            ? "Settings are saved locally. If mouse pass-through is enabled, use the notification-area menu to restore control."
            : "设置保存在本机。如果开启鼠标穿透，请使用通知区域菜单恢复控制。";
        MessageBox.Show(this, message, Preferences.Language == "en-US" ? "Quota Float Help" : "Quota Float 帮助", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Reset_OnClick(object sender, RoutedEventArgs e)
    {
        preferenceStore.Reset();
        LoadControlsFromPreferences();
        owner.ApplyLanguage();
        SaveAndUpdateIndicator();
    }

    private void Done_OnClick(object sender, RoutedEventArgs e)
    {
        closingFromDone = true;
        SaveAndUpdateIndicator();
        Close();
    }

    private void SaveAndUpdateIndicator()
    {
        preferenceStore.Save();
        SavedStateText.Text = Preferences.Language == "en-US" ? "Saved · just now" : "已保存 · 刚刚";
        FooterSavedText.Text = SavedStateText.Text;
    }

    private void UpdateFullSizeText()
    {
        FullSizeValueText.Text = $"{ProductScaleRules.NormalizePercent((int)Math.Round(FullSizeSlider.Value, MidpointRounding.AwayFromZero))}%";
    }

    private void ApplyLanguage()
    {
        var english = Preferences.Language == "en-US";
        var uiFont = new FontFamily(english ? "Segoe UI" : "Microsoft YaHei UI, Segoe UI");
        Title = english ? "Quota Float Preferences" : "Quota Float 偏好设置";
        AutomationProperties.SetName(this, english ? "Preferences" : "偏好设置");
        TitleText.Text = english ? "Preferences" : "偏好设置";
        SubtitleText.Text = english ? "Saved locally · changes apply immediately" : "本地保存 · 修改即时生效";
        ViewLabelText.Text = english ? "Default view" : "默认窗口";
        LanguageLabelText.Text = english ? "Language" : "语言";
        FullSizeLabelText.Text = english ? "Full size" : "Full 大小";
        MinSizeText.Text = "40%";
        MaxSizeText.Text = "100%";
        RestoreFullSizeButton.Content = english ? "Restore 100%" : "恢复 100%";
        AppearanceLabelText.Text = english ? "Windows appearance" : "Windows 外观";
        AppearanceNoteText.Text = english ? "Uses system DPI, fonts and high contrast; no independent theme selector." : "使用系统 DPI、字体与高对比度；不提供独立主题选择器。";
        AlwaysTopLabelText.Text = english ? "Always on top" : "始终置顶";
        AlwaysTopNoteText.Text = english ? "Enabled by default" : "默认开启";
        EdgeAutoOrbLabelText.Text = english ? "Edge becomes Orb" : "贴边变悬浮球";
        EdgeAutoOrbNoteText.Text = english ? "24 px threshold" : "24 px 触发";
        HoverExpandLabelText.Text = english ? "Hover expands Full" : "悬停展开 Full";
        HoverExpandNoteText.Text = "300 ms";
        ClickThroughLabelText.Text = english ? "Mouse pass-through" : "鼠标穿透";
        ClickThroughNoteText.Text = english ? "Off after restart" : "重启自动关闭";
        AutoRefreshLabelText.Text = english ? "Auto refresh" : "自动刷新";
        AutoRefreshNoteText.Text = english ? "Backs off on failure" : "失败时退避";
        FollowLifecycleLabelText.Text = english ? "Follow Codex lifecycle" : "跟随 Codex 生命周期";
        FollowLifecycleNoteText.Text = english ? "Exit after all Codex windows close" : "Codex 全部关闭后退出";
        LowQuotaLabelText.Text = english ? "Low-quota alerts" : "低额度提醒";
        LowQuotaNoteText.Text = english ? "Threshold is in Advanced" : "阈值在高级设置";
        RefreshIntervalLabelText.Text = english ? "Refresh interval" : "刷新间隔";
        RefreshIntervalHost.Width = 230;
        RefreshIntervalHost.HorizontalAlignment = HorizontalAlignment.Left;
        RefreshIntervalInput.Width = 174;
        RefreshIntervalInput.HorizontalAlignment = HorizontalAlignment.Left;
        RefreshSecondsText.Text = english ? "seconds" : "秒";
        HelpButton.Content = english ? "Help" : "帮助";
        ResetButton.Width = english ? 94 : 58;
        ResetButton.Content = english ? "Reset preferences" : "重置偏好";
        DoneButton.Content = english ? "Done" : "完成";
        SavedStateText.Text = english ? "Saved · just now" : "已保存 · 刚刚";
        FooterSavedText.Text = SavedStateText.Text;
        ApplyAccessibilityNames(english);
        ApplyLanguageFont(uiFont);
    }

    private void ApplyAccessibilityNames(bool english)
    {
        AutomationProperties.SetName(FullViewButton, english ? "Default view: Full" : "默认窗口：Full");
        AutomationProperties.SetName(OrbViewButton, english ? "Default view: Orb" : "默认窗口：Orb");
        AutomationProperties.SetName(ChineseLanguageButton, english ? "Language: Simplified Chinese" : "语言：简体中文");
        AutomationProperties.SetName(EnglishLanguageButton, english ? "Language: English" : "语言：English");
        AutomationProperties.SetName(FullSizeSlider, english ? "Full size" : "Full 大小");
        AutomationProperties.SetName(RestoreFullSizeButton, english ? "Restore Full size to 100 percent" : "恢复 Full 大小到 100%" );
        AutomationProperties.SetName(AlwaysTopToggle, english ? "Always on top" : "始终置顶");
        AutomationProperties.SetName(EdgeAutoOrbToggle, english ? "Edge becomes Orb" : "贴边变悬浮球");
        AutomationProperties.SetName(HoverExpandToggle, english ? "Hover expands Full" : "悬停展开 Full");
        AutomationProperties.SetName(ClickThroughToggle, english ? "Mouse pass-through" : "鼠标穿透");
        AutomationProperties.SetName(AutoRefreshToggle, english ? "Auto refresh" : "自动刷新");
        AutomationProperties.SetName(FollowLifecycleToggle, english ? "Follow Codex lifecycle" : "跟随 Codex 生命周期");
        AutomationProperties.SetName(LowQuotaToggle, english ? "Low-quota alerts" : "低额度提醒");
        AutomationProperties.SetName(RefreshIntervalInput, english ? "Refresh interval in seconds" : "刷新间隔（秒）");
        AutomationProperties.SetName(HelpButton, english ? "Help" : "帮助");
        AutomationProperties.SetName(ResetButton, english ? "Reset preferences" : "重置偏好");
        AutomationProperties.SetName(DoneButton, english ? "Done" : "完成");
    }

    private void ApplyLanguageFont(FontFamily uiFont)
    {
        foreach (var element in new TextBlock[]
        {
            TitleText, SubtitleText, SavedStateText, ViewLabelText, LanguageLabelText, FullSizeLabelText,
            MinSizeText, MaxSizeText, AppearanceLabelText, AppearanceNoteText, AlwaysTopLabelText,
            AlwaysTopNoteText, EdgeAutoOrbLabelText, EdgeAutoOrbNoteText, HoverExpandLabelText,
            HoverExpandNoteText, ClickThroughLabelText, ClickThroughNoteText, AutoRefreshLabelText,
            AutoRefreshNoteText, FollowLifecycleLabelText, FollowLifecycleNoteText, LowQuotaLabelText,
            LowQuotaNoteText, FooterSavedText, FullSizeValueText
        })
        {
            element.FontFamily = uiFont;
        }

        foreach (var control in new Control[]
        {
            FullViewButton, OrbViewButton, ChineseLanguageButton, EnglishLanguageButton,
            RestoreFullSizeButton, HelpButton, ResetButton, DoneButton
        })
        {
            control.FontFamily = uiFont;
        }

        RefreshIntervalInput.FontFamily = new FontFamily("Segoe UI");
        RefreshIntervalLabelText.FontFamily = new FontFamily("Microsoft YaHei UI, Segoe UI");
        RefreshSecondsText.FontFamily = new FontFamily("Microsoft YaHei UI, Segoe UI");
    }

    private void FocusFirstActionableControl()
    {
        Dispatcher.BeginInvoke(new Action(() => Keyboard.Focus(FullViewButton)), System.Windows.Threading.DispatcherPriority.Input);
    }
}
