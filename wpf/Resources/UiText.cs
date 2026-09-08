using QuotaFloat;
using QuotaFloat.Wpf.Services;

namespace QuotaFloat.Wpf.Resources;

public sealed record WidgetText(
    string FullFiveHourLabel,
    string OrbFiveHourLabel,
    string FiveHourRemaining,
    string WeeklyLabel,
    string OrbWeeklyLabel,
    string WeeklyRemaining,
    string ResetCredits,
    string EarliestExpiry,
    string Billing,
    string Synced,
    string Refresh,
    string Settings,
    string BillingAutomationName,
    string SyncAutomationName,
    string RefreshAutomationName,
    string SettingsAutomationName,
    string Loading,
    string Refreshing,
    string SignedOut,
    string Offline,
    string RateLimited,
    string Malformed,
    string SessionChanged,
    string Partial,
    string Stale,
    string Cancelled,
    string Unknown,
    string NoValue)
{
    public static WidgetText For(string language) => string.Equals(language, "en-US", StringComparison.OrdinalIgnoreCase)
        ? English
        : SimplifiedChinese;

    private static readonly WidgetText SimplifiedChinese = new(
        FullFiveHourLabel: "5 小时",
        OrbFiveHourLabel: "5小时",
        FiveHourRemaining: "剩余 2小时17分",
        WeeklyLabel: "每周",
        OrbWeeklyLabel: "每周",
        WeeklyRemaining: "剩余 2天23小时",
        ResetCredits: "重置机会 2",
        EarliestExpiry: "最早到期 9月16日",
        Billing: "用量与账单 →",
        Synced: "已同步",
        Refresh: "刷新",
        Settings: "设置",
        BillingAutomationName: "用量与账单",
        SyncAutomationName: "已同步",
         RefreshAutomationName: "刷新",
         SettingsAutomationName: "设置",
         Loading: "加载中",
         Refreshing: "刷新中",
         SignedOut: "未登录",
         Offline: "离线",
         RateLimited: "请求受限",
         Malformed: "数据格式异常",
         SessionChanged: "会话已变化",
         Partial: "部分同步",
         Stale: "数据过期",
         Cancelled: "已取消",
         Unknown: "未知",
         NoValue: "—");

    private static readonly WidgetText English = new(
        FullFiveHourLabel: "5-hour",
        OrbFiveHourLabel: "5h",
        FiveHourRemaining: "2h 17m remaining",
        WeeklyLabel: "Weekly",
        OrbWeeklyLabel: "Wk",
        WeeklyRemaining: "2d 23h remaining",
        ResetCredits: "Reset credits 2",
        EarliestExpiry: "Earliest expiry Sep 16",
        Billing: "Usage & billing →",
        Synced: "Synced",
        Refresh: "Refresh",
        Settings: "Settings",
        BillingAutomationName: "Usage & billing",
        SyncAutomationName: "Synced",
         RefreshAutomationName: "Refresh",
         SettingsAutomationName: "Settings",
         Loading: "Loading",
         Refreshing: "Refreshing",
         SignedOut: "Signed out",
         Offline: "Offline",
         RateLimited: "Rate limited",
         Malformed: "Malformed data",
         SessionChanged: "Session changed",
         Partial: "Partial",
         Stale: "Stale",
         Cancelled: "Cancelled",
         Unknown: "Unknown",
         NoValue: "—");

    public string Status(QuotaUiStatus status, bool isRefreshing = false) => isRefreshing ? Refreshing : status switch
    {
        QuotaUiStatus.Loading => Loading,
        QuotaUiStatus.SignedOut => SignedOut,
        QuotaUiStatus.Offline => Offline,
        QuotaUiStatus.RateLimited => RateLimited,
        QuotaUiStatus.Malformed => Malformed,
        QuotaUiStatus.SessionChanged => SessionChanged,
        QuotaUiStatus.Partial => Partial,
        QuotaUiStatus.Stale => Stale,
        QuotaUiStatus.Cancelled => Cancelled,
        _ => status == QuotaUiStatus.Fresh ? Synced : Unknown
    };

    public string Remaining(QuotaWindow window, bool compact)
    {
        var english = ReferenceEquals(this, English);
        if (window.ResetAt is not { } resetAt) return compact ? NoValue : (english ? "Remaining unknown" : "剩余未知");
        var remaining = resetAt - DateTimeOffset.Now;
        if (remaining <= TimeSpan.Zero) return compact ? NoValue : (english ? "Resetting" : "等待重置");
        if (remaining.TotalDays >= 1) return english ? $"{(int)remaining.TotalDays}d {remaining.Hours}h remaining" : $"剩余 {(int)remaining.TotalDays}天{remaining.Hours}小时";
        return english ? $"{remaining.Hours}h {remaining.Minutes}m remaining" : $"剩余 {remaining.Hours}小时{remaining.Minutes}分";
    }
}
