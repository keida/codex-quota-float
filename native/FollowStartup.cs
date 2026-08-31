using Microsoft.Win32;

namespace QuotaFloat;

internal static class FollowStartup
{
    const string KeyPath=@"Software\Microsoft\Windows\CurrentVersion\Run";
    const string ValueName="QuotaFloat.FollowCodex";
    static string Command=>"\""+Application.ExecutablePath+"\" --watch";
    public static bool Enabled
    {
        get{try{using var key=Registry.CurrentUser.OpenSubKey(KeyPath);return string.Equals(key?.GetValue(ValueName) as string,Command,StringComparison.OrdinalIgnoreCase);}catch{return false;}}
    }
    // Called only after the user explicitly enables/disables the labeled setting.
    public static void SetEnabled(bool enabled)
    {
        using var key=Registry.CurrentUser.CreateSubKey(KeyPath,true);
        var existing=key.GetValue(ValueName) as string;
        if(existing is not null&&!string.Equals(existing,Command,StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("存在其他位置的 Quota Float 启动项，请先从旧版本关闭联动。 / Disable following in the old installation first.");
        if(enabled)key.SetValue(ValueName,Command,RegistryValueKind.String);
        else if(existing is not null)key.DeleteValue(ValueName,false);
        FollowContext.Current?.SetKeepWatching(enabled);
    }
}
