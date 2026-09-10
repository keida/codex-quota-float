namespace QuotaFloat.Wpf.Services;

public enum WpfLaunchMode
{
    LaunchAndWatch,
    Watch,
    Direct,
    Invalid
}

public readonly record struct LaunchOptions(WpfLaunchMode Mode, string? Error)
{
    public bool IsValid => Mode != WpfLaunchMode.Invalid;
    public bool LaunchCodexIfMissing => Mode == WpfLaunchMode.LaunchAndWatch;

    public static LaunchOptions Parse(string[] args)
    {
        var direct = args.Any(a => string.Equals(a, "--direct", StringComparison.OrdinalIgnoreCase));
        var watch = args.Any(a => string.Equals(a, "--watch", StringComparison.OrdinalIgnoreCase));
        if (direct && watch)
        {
            return new(WpfLaunchMode.Invalid, "Use either --watch or --direct, not both.");
        }

        if (direct) return new(WpfLaunchMode.Direct, null);
        if (watch) return new(WpfLaunchMode.Watch, null);
        return new(WpfLaunchMode.LaunchAndWatch, null);
    }
}
