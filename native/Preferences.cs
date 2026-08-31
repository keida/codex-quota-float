using System.Text.Json;

namespace QuotaFloat;

public sealed class Preferences
{
    public string Theme {get;set;}="dark";
    public string Skin {get;set;}="ink";
    public string Language {get;set;}="zh";
    public string Mode {get;set;}="full";
    public int FontSize {get;set;}=12;
    public int FullScalePercent {get;set;}=100;
    public bool Comfortable {get;set;}
    public bool Topmost {get;set;}=true;
    public bool HoverExpand {get;set;}=true;
    public bool KeepExpanded {get;set;}
    public bool ClickThrough {get;set;}
    public bool Snap {get;set;}=true;
    public bool EdgeCollapse {get;set;}=true;
    public bool AutoRefresh {get;set;}=true;
    public int RefreshSeconds {get;set;}=300;
    public int Warning {get;set;}=50;
    public int Critical {get;set;}=10;
    public bool Notify {get;set;}
    public int? X {get;set;}
    public int? Y {get;set;}
    public void Validate()
    {
        if(!new[]{"dark","light","system"}.Contains(Theme))Theme="dark";
        if(!new[]{"ink","soft","terminal"}.Contains(Skin))Skin="ink";
        if(!new[]{"zh","en"}.Contains(Language))Language="zh";
        if(!new[]{"full","mini","orb"}.Contains(Mode))Mode="full";
        FontSize=Math.Clamp(FontSize,11,14);FullScalePercent=Math.Clamp(FullScalePercent,100,180);RefreshSeconds=Math.Clamp(RefreshSeconds,60,1800);
        Warning=Math.Clamp(Warning,2,99);Critical=Math.Clamp(Critical,1,Warning-1);
        // Never restore a locked window on launch; keep an immediately usable recovery path.
        ClickThrough=false;
    }
}
public sealed class PreferenceStore
{
    public static string DirectoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"QuotaFloat");
    readonly string file;
    readonly string directory;
    readonly bool enabled;
    public string? Warning {get;private set;}
    public PreferenceStore(bool enabled,string? testDirectory=null){this.enabled=enabled;directory=testDirectory??DirectoryPath;file=Path.Combine(directory,"preferences.json");}
    public Preferences Load()
    {
        if(!enabled)return new();
        foreach(var candidate in new[]{file,file+".bak"})
        {
            try
            {
                if(!File.Exists(candidate))continue;
                if(new FileInfo(candidate).Length>32768)throw new InvalidDataException();
                var prefs=JsonSerializer.Deserialize<Preferences>(File.ReadAllText(candidate))??throw new InvalidDataException();
                prefs.Validate();if(candidate.EndsWith(".bak"))Warning="配置损坏，已恢复备份。";return prefs;
            }catch(Exception e)when(e is IOException or InvalidDataException or UnauthorizedAccessException or JsonException){Warning="配置无法读取，已使用安全默认值。";}
        }
        return new();
    }
    public bool Save(Preferences prefs)
    {
        if(!enabled)return true;
        try
        {
            // Validate an isolated copy, without changing live click-through state.
            var safe=JsonSerializer.Deserialize<Preferences>(JsonSerializer.Serialize(prefs))??new();safe.Validate();
            Directory.CreateDirectory(directory);var temp=file+".tmp";
            File.WriteAllText(temp,JsonSerializer.Serialize(safe,new JsonSerializerOptions{WriteIndented=true}));
            if(File.Exists(file))File.Replace(temp,file,file+".bak");else File.Move(temp,file);
            Warning=null;return true;
        }catch(Exception e)when(e is IOException or UnauthorizedAccessException){Warning="配置无法保存；当前设置仅本次生效。";return false;}
    }
}
