using System.Text.Json;
using QuotaFloat;

int passed=0;
void Check(bool condition,string name){if(!condition)throw new Exception("FAIL: "+name);Console.WriteLine("PASS: "+name);passed++;}
QuotaSnapshot? Parse(string json,string credits="{}")
{
    using var u=JsonDocument.Parse(json);using var c=JsonDocument.Parse(credits);return QuotaParser.Parse(u.RootElement,c.RootElement,DateTimeOffset.UtcNow);
}
var both=Parse("""{"plan_type":"pro","rate_limit":{"primary_window":{"used_percent":28,"limit_window_seconds":18000,"reset_at":1788208800},"secondary_window":{"remaining_percent":38,"limit_window_seconds":604800}}}""","""{"available_count":2,"credits":[{"expires_at":1788208800},{"expires_at":"2026-09-30T00:00:00Z"}]}""");
Check(both?.Short?.Remaining==72&&both.Weekly?.Remaining==38&&both.Credits==2&&both.Expiries.Length==2,"Both windows, count and expiry");
var weekly=Parse("""{"rate_limit":{"primary_window":{"remaining_percent":98,"limit_window_seconds":604800}}}""");
Check(weekly?.Short is null&&weekly?.Weekly?.Remaining==98,"Weekly primary never becomes 5h");
var noDuration=Parse("""{"rate_limit":{"primary_window":{"remaining_percent":25}}}""");
Check(noDuration?.Short?.Remaining==25&&noDuration.Weekly is null,"Undocumented primary duration not duplicated as weekly");
var ratio=Parse("""{"rate_limit":{"primary_window":{"remaining_ratio":0.25},"secondary_window":{"used_percent":0.4}}}""");
Check(ratio?.Short?.Remaining==25&&ratio.Weekly?.Remaining==99.6,"Ratio and literal fractional percent distinguished");
Check(Parse("""{"rate_limit":{"primary_window":{"remaining_percent":-1}}}""") is null,"Negative percentage rejected");
Check(Parse("""{"rate_limit":{"primary_window":{"used_percent":101}}}""") is null,"Over-range percentage rejected");
Check(Parse("""{"some_future_format":{}}""") is null,"Unknown schema fails closed");
var missing=Parse("""{"rate_limit":{"primary_window":{"used_percent":100}}}""");
Check(missing?.Short?.Remaining==0&&missing.Credits is null&&missing.Partial,"Real zero distinguished from unknown credits");
var array=Parse("""{"rate_limit":{"windows":[{"name":"primary","remaining_percent":82,"window_seconds":604800}]}}""");
Check(array?.Short is null&&array?.Weekly?.Remaining==82,"Array duration overrides contradictory name");
var badCount=Parse("""{"primary":{"remaining_percent":70}}""","""{"available_count":2.5}""");
Check(badCount?.Credits is null,"Fractional credit count rejected");
Check(Parse("""{"rate_limit":{"primary_window":{"remaining_percent":80},"primaryWindow":{"remaining_percent":20}}}""") is null,"Conflicting primary aliases rejected");
Check(Parse("""{"primary":{"remaining_percent":80,"remainingPercent":20}}""") is null,"Conflicting metric aliases rejected");
Check(Parse("""{"rate_limit":{"secondary_window":{"remaining_percent":80},"weekly_window":{"remaining_percent":20}}}""") is null,"Conflicting weekly aliases rejected");
var prefs=new Preferences{Theme="bad",Mode="bad",FontSize=90,FullScalePercent=999,Warning=-1,Critical=90,RefreshSeconds=0,ClickThrough=true};prefs.Validate();
Check(prefs.Theme=="dark"&&prefs.Mode=="full"&&prefs.FullScalePercent==180&&prefs.Critical<prefs.Warning&&!prefs.ClickThrough&&prefs.RefreshSeconds==60,"Corrupt preferences normalize and unlock safely");
var fixture=Path.Combine(Path.GetTempPath(),"QuotaFloat-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(fixture);
try
{
    var preferenceFile=Path.Combine(fixture,"preferences.json");File.WriteAllText(preferenceFile,new string('x',33000));
    var store=new PreferenceStore(true,fixture);Check(store.Load().Theme=="dark"&&store.Warning is not null,"Oversized preference file does not crash startup");
    var invalid=new Preferences{Theme=new string('x',40000),ClickThrough=true,FullScalePercent=-1,RefreshSeconds=-1};
    Check(store.Save(invalid)&&new FileInfo(preferenceFile).Length<32768&&invalid.ClickThrough&&store.Load().RefreshSeconds==60&&store.Load().FullScalePercent==100,"Save normalizes isolated copy without mutating live lock");
}
finally{foreach(var file in Directory.GetFiles(fixture))File.Delete(file);Directory.Delete(fixture);}
if(args.Contains("--windows")){WindowRegression.Run();Check(true,"Settings remains above widget through live mode change");}
if(args.Contains("--resize-snap")){ResizeSnapRegression.Run();Check(true,"Resize, edge snap and orb discoverability regression");}
if(args.Contains("--follow"))
{
    int pathIndex=Array.IndexOf(args,"--host-path");
    if(pathIndex<0||pathIndex+1>=args.Length)throw new ArgumentException("--follow requires --host-path for the controlled host executable");
    FollowRegression.Run(Path.GetFullPath(args[pathIndex+1]));Check(true,"Real window/process close and reopen follow cycle");
}
if(args.Contains("--live-presence"))
{
    using var lifecycle=new CodexLifecycle();var presence=lifecycle.RefreshPresence();
    Console.WriteLine($"LIVE presence={presence.IsPresent}, windows={presence.VisibleWindowCount}, matching desktop processes={presence.ProcessIds.Count}");
    Check(presence.IsPresent,"Cold watcher discovery sees currently open official Codex desktop windows");
}
Console.WriteLine($"{passed} checks passed; no account or network access.");
