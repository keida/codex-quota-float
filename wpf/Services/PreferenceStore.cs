using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace QuotaFloat.Wpf.Services;

public sealed class UserPreferences
{
    public string Language { get; set; } = "zh-Hans";
    public int RefreshIntervalSeconds { get; set; } = 30;
}

public sealed class PreferenceStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string DefaultPath => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "QuotaFloat",
        "preferences.json");

    public PreferenceStore(UserPreferences current, string path)
    {
        Current = Normalize(current);
        Path = path;
    }

    public UserPreferences Current { get; private set; }

    public string Path { get; }

    public static PreferenceStore Load(string? path = null)
    {
        var resolvedPath = string.IsNullOrWhiteSpace(path) ? DefaultPath : path;
        try
        {
            if (File.Exists(resolvedPath))
            {
                var loaded = JsonSerializer.Deserialize<UserPreferences>(File.ReadAllText(resolvedPath), SerializerOptions);
                if (loaded is not null)
                {
                    return new PreferenceStore(loaded, resolvedPath);
                }
            }
        }
        catch (IOException)
        {
            // A malformed or unavailable preference file must not prevent the widget from starting.
        }
        catch (JsonException)
        {
            // Obsolete or malformed preference data is ignored and replaced with supported defaults.
        }

        return new PreferenceStore(new UserPreferences(), resolvedPath);
    }

    public void Save()
    {
        Current = Normalize(Current);
        var directory = System.IO.Path.GetDirectoryName(Path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(Path, JsonSerializer.Serialize(Current, SerializerOptions));
    }

    private static UserPreferences Normalize(UserPreferences source)
    {
        source.Language = string.Equals(source.Language, "en-US", StringComparison.OrdinalIgnoreCase)
            ? "en-US"
            : "zh-Hans";
        source.RefreshIntervalSeconds = NearestRefreshInterval(source.RefreshIntervalSeconds);
        return source;
    }

    public static readonly int[] ApprovedRefreshIntervals = [15, 30, 60, 120, 300];

    public static int NearestRefreshInterval(int value)
    {
        if (value <= 0) return 30;
        return ApprovedRefreshIntervals
            .OrderBy(candidate => Math.Abs(candidate - value))
            .ThenBy(candidate => candidate)
            .First();
    }
}
