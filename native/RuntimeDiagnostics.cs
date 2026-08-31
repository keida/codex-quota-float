namespace QuotaFloat;

// Small local breadcrumb log, never exception messages, stacks, account data or HTTP bodies.
internal static class RuntimeDiagnostics
{
    static readonly object Sync=new();
    public static bool Enabled {get;set;}=true;
    public static void Record(string reason,Exception? exception=null)
    {
        if(!Enabled)return;
        try
        {
            lock(Sync)
            {
                Directory.CreateDirectory(PreferenceStore.DirectoryPath);
                var path=Path.Combine(PreferenceStore.DirectoryPath,"lifecycle.log");
                var previous=File.Exists(path)&&new FileInfo(path).Length<=8192?File.ReadAllLines(path).TakeLast(23):[];
                var entry=$"{DateTimeOffset.UtcNow:O} {reason}"+(exception is null?"":" type="+exception.GetType().Name);
                File.WriteAllLines(path,previous.Append(entry));
            }
        }
        catch(Exception e)when(e is IOException or UnauthorizedAccessException){}
    }
}
