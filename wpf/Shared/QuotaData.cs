using System.Globalization;
using System.Text.Json;

namespace QuotaFloat;

public sealed record QuotaWindow(double Remaining, DateTimeOffset? ResetAt, long Seconds, string? Label = null);
public sealed record QuotaSnapshot(string? Plan, QuotaWindow? Short, QuotaWindow? Weekly,
    int? Credits, DateTimeOffset[] Expiries, DateTimeOffset UpdatedAt, QuotaWindow[]? Windows = null)
{
    public bool Partial => VisibleWindows.Count==0 || Credits is null;
    public IReadOnlyList<QuotaWindow> VisibleWindows =>
        Windows is { Length: > 0 } ? Windows : new[]{Short,Weekly}.Where(x=>x is not null).Cast<QuotaWindow>().ToArray();
    public DateTimeOffset? EarliestFutureExpiry(DateTimeOffset now)
    {
        var future=Expiries.Where(x=>x>now).Order().ToArray();
        return future.Length==0?null:future[0];
    }
}
public sealed record QuotaResult(QuotaSnapshot? Snapshot, string Status, TimeSpan? RetryAfter = null);

// Independent implementation of observed JSON fields; unknown values never become zero quota.
public static class QuotaParser
{
    public static string? NormalizePlan(string? value)
    {
        if(string.IsNullOrWhiteSpace(value))return null;
        var compact=new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        return compact switch
        {
            "PRO" or "CHATGPTPRO" => "Pro",
            "PLUS" or "CHATGPTPLUS" => "Plus",
            "GO" or "CHATGPTGO" => "Go",
            _ => null
        };
    }
    public static JsonElement Field(JsonElement value, params string[] names)
    {
        if(value.ValueKind != JsonValueKind.Object)return default;
        JsonElement found=default;int count=0;
        foreach(var property in value.EnumerateObject())if(names.Contains(property.Name)){found=property.Value;count++;}
        return count==1?found:default;
    }
    public static string? Text(JsonElement value, params string[] names)
    {
        var field=Field(value,names);return field.ValueKind==JsonValueKind.String?field.GetString():null;
    }
    static double? Number(JsonElement value) => value.ValueKind==JsonValueKind.Number && value.TryGetDouble(out var n) && double.IsFinite(n)?n:null;
    public static DateTimeOffset? Timestamp(JsonElement value)
    {
        if(value.ValueKind==JsonValueKind.String && DateTimeOffset.TryParse(value.GetString(),CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out var date))return date;
        if(value.ValueKind==JsonValueKind.Number && value.TryGetInt64(out var n))
            try {return DateTimeOffset.FromUnixTimeSeconds(n);} catch(ArgumentOutOfRangeException){}
        return null;
    }
    static (double Number,string Key)? Metric(JsonElement value, params string[] names)
    {
        foreach(var key in names)if(Number(Field(value,key)) is double number)return (number,key);
        return null;
    }
    public static QuotaWindow? Window(JsonElement value)
    {
        if(value.ValueKind!=JsonValueKind.Object)return null;
        string[][] aliases=[
            ["remaining_percent","remainingPercent","remaining_pct","remainingPct","remaining_ratio","remainingRatio","remaining"],
            ["used_percent","usedPercent","used_pct","usedPct","used_ratio","usedRatio","utilization","used"],
            ["limit_window_seconds","limitWindowSeconds","window_seconds","windowSeconds","duration_seconds","durationSeconds","period_seconds","periodSeconds"]
        ];
        if(aliases.Any(group=>value.EnumerateObject().Count(p=>group.Contains(p.Name))>1))return null;
        var remaining=Metric(value,"remaining_percent","remainingPercent","remaining_pct","remainingPct","remaining_ratio","remainingRatio","remaining");
        var used=Metric(value,"used_percent","usedPercent","used_pct","usedPct","used_ratio","usedRatio","utilization","used");
        var selected=remaining??used;if(selected is null)return null;
        var (n,key)=selected.Value;
        bool ratio=key.Contains("ratio",StringComparison.OrdinalIgnoreCase)||key=="utilization"||((key=="used"||key=="remaining")&&n<=1);
        if(ratio)n*=100;
        if(n<0||n>100)return null;
        var seconds=Number(Field(value,"limit_window_seconds","limitWindowSeconds","window_seconds","windowSeconds","duration_seconds","durationSeconds","period_seconds","periodSeconds"));
        if(seconds is <0 || seconds>long.MaxValue)return null;
        return new(remaining is null?100-n:n,Timestamp(Field(value,"reset_at","resetAt","resets_at","resetsAt","reset_time","resetTime")),(long)(seconds??0));
    }
    static QuotaWindow? FindWindow(JsonElement limits,long seconds,string[] aliases)
    {
        if(limits.ValueKind!=JsonValueKind.Object)return null;
        if(limits.EnumerateObject().Count(p=>aliases.Contains(p.Name))>1)return null;
        foreach(var alias in aliases)
        {
            var window=Window(Field(limits,alias));
            if(window is not null&&(window.Seconds==0||Math.Abs(window.Seconds-seconds)<=60))return window;
        }
        foreach(var key in new[]{"windows","limit_windows","limitWindows","limits","buckets"})
        {
            var array=Field(limits,key);if(array.ValueKind!=JsonValueKind.Array)continue;
            foreach(var item in array.EnumerateArray())
            {
                var window=Window(item);if(window is null)continue;
                if(window.Seconds>0) {if(Math.Abs(window.Seconds-seconds)<=60)return window;continue;}
                var name=Text(item,"name","type","id","window","label");
                if(name is not null&&aliases.Contains(name,StringComparer.OrdinalIgnoreCase))return window;
            }
        }
        return null;
    }
    static int? Count(JsonElement value)
    {
        var n=Number(Field(value,"available_count","availableCount","remaining","count","quantity"));
        return n is >=0 and <=int.MaxValue && n==Math.Truncate(n.Value)?(int)n:null;
    }
    static DateTimeOffset[] Expirations(JsonElement root)
    {
        var result=new List<DateTimeOffset>();
        void Visit(JsonElement node,int depth)
        {
            if(depth>12||result.Count>1000)return;
            if(node.ValueKind==JsonValueKind.Array){foreach(var item in node.EnumerateArray())Visit(item,depth+1);return;}
            if(node.ValueKind!=JsonValueKind.Object)return;
            if(Timestamp(Field(node,"expires_at","expiresAt","expiration_time","expirationTime","expires")) is {} date)result.Add(date);
            foreach(var key in new[]{"credits","reset_credits","resetCredits","available","items","grants"})Visit(Field(node,key),depth+1);
        }
        Visit(root,0);return result.Distinct().Order().ToArray();
    }
    static string WindowLabel(JsonElement item, long seconds, bool weekly)
    {
        var name=Text(item,"name","type","id","window","label");
        if(name is not null)
        {
            if(name.Contains("week",StringComparison.OrdinalIgnoreCase)||name.Contains("secondary",StringComparison.OrdinalIgnoreCase))return "Weekly";
            if(name.Contains("5h",StringComparison.OrdinalIgnoreCase)||name.Contains("five",StringComparison.OrdinalIgnoreCase)||name.Contains("primary",StringComparison.OrdinalIgnoreCase))return "5h";
            return name.Length>22?name[..22]:name;
        }
        return weekly?"Weekly":seconds is >=17940 and <=18060?"5h":"Window";
    }
    static QuotaWindow[] DiscoverWindows(JsonElement limits, QuotaWindow? shortWindow, QuotaWindow? weekly)
    {
        var result=new List<QuotaWindow>();
        void Add(QuotaWindow? window,string label){if(window is null)return;result.Add(window with {Label=label});}
        Add(shortWindow,"5h");Add(weekly,"Weekly");
        foreach(var key in new[]{"windows","limit_windows","limitWindows","limits","buckets"})
        {
            var array=Field(limits,key);if(array.ValueKind!=JsonValueKind.Array)continue;
            foreach(var item in array.EnumerateArray())
            {
                var window=Window(item);if(window is null)continue;
                var label=WindowLabel(item,window.Seconds,window.Seconds is >=604740 and <=604860);
                if(result.Any(x=>x.Seconds==window.Seconds&&x.ResetAt==window.ResetAt))continue;
                result.Add(window with {Label=label});
            }
        }
        return result.ToArray();
    }
    public static QuotaSnapshot? Parse(JsonElement usage,JsonElement credits,DateTimeOffset now)
    {
        var limits=Field(usage,"rate_limit","rateLimit");if(limits.ValueKind==JsonValueKind.Undefined)limits=usage;
        var shortWindow=FindWindow(limits,18000,["primary_window","primaryWindow","short_window","shortWindow","five_hour_window","fiveHourWindow","5h","primary"]);
        // Primary is a weekly fallback only when it explicitly carries a weekly duration.
        var weekly=FindWindow(limits,604800,["secondary_window","secondaryWindow","weekly_window","weeklyWindow","week_window","weekWindow","weekly","secondary"]);
        if(weekly is null){var primary=Window(Field(limits,"primary_window","primaryWindow","primary"));if(primary?.Seconds is >=604740 and <=604860)weekly=primary;}
        var windows=DiscoverWindows(limits,shortWindow,weekly);
        if(windows.Length==0)return null;
        var fallback=Field(usage,"rate_limit_reset_credits","rateLimitResetCredits");
        var expiries=Expirations(credits);if(expiries.Length==0)expiries=Expirations(fallback);
        var rawPlan=Text(usage,"plan_type","planType");
        var plan=NormalizePlan(rawPlan)??(string.IsNullOrWhiteSpace(rawPlan)?null:"Other");
        var actualShort=windows.FirstOrDefault(x=>x.Label=="5h")??shortWindow;
        var actualWeekly=windows.FirstOrDefault(x=>x.Label=="Weekly")??weekly;
        return new(plan,actualShort,actualWeekly,Count(credits)??Count(fallback),expiries,now,windows);
    }
}
