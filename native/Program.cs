using System.Diagnostics;
using System.Text.Json;

namespace QuotaFloat;

internal static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        RuntimeDiagnostics.Enabled=!args.Contains("--demo")&&!args.Contains("--test-host");
        Application.ThreadException+=(_,e)=>{RuntimeDiagnostics.Record("ui-fault",e.Exception);Application.Exit();};
        AppDomain.CurrentDomain.UnhandledException+=(_,e)=>RuntimeDiagnostics.Record("unhandled-fault",e.ExceptionObject as Exception);
        string? Value(string key){var i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:null;}
        string? reportPath=null;
        if(Value("--report") is {} name)
        {
            // Diagnostic reports are opt-in and confined to this executable's own directory.
            if(Path.GetFileName(name)!=name||!name.EndsWith(".json",StringComparison.OrdinalIgnoreCase)||name.Length>80||name.IndexOfAny(Path.GetInvalidFileNameChars())>=0)return 4;
            var diagnostics=Path.Combine(AppContext.BaseDirectory,"diagnostics");Directory.CreateDirectory(diagnostics);reportPath=Path.Combine(diagnostics,name);
        }
        if(args.Contains("--test-host"))
        {
            using var testHost=new Form{Text="Quota Float lifecycle test host",Width=300,Height=120,ShowInTaskbar=false};
            using var lifetime=new System.Windows.Forms.Timer{Interval=20000};lifetime.Tick+=(_,_)=>testHost.Close();lifetime.Start();Application.Run(testHost);return 0;
        }
        bool demo=args.Contains("--demo");
        int.TryParse(Value("--test-parent"),out var testParent);
        if(testParent>0&&!demo){MessageBox.Show("--test-parent requires --demo; no live credentials are used in tests.","Quota Float");return 2;}
        if(args.Contains("--probe"))
        {
            using var probeClient=new QuotaClient();
            var result=probeClient.FetchAsync(CancellationToken.None).GetAwaiter().GetResult();
            // Only normalized, non-identifying values. Never emit response bodies, credentials or account IDs.
            if(reportPath is {} report)File.WriteAllText(report,JsonSerializer.Serialize(new{result.Status,Short=result.Snapshot?.Short?.Remaining,Weekly=result.Snapshot?.Weekly?.Remaining,Credits=result.Snapshot?.Credits,At=result.Snapshot?.UpdatedAt},new JsonSerializerOptions{WriteIndented=true}));
            return result.Snapshot is null?3:0;
        }
        using var mutex=new Mutex(false,demo?"Local\\QuotaFloat.Demo.v1":"Local\\QuotaFloat.Desktop.v1");
        bool acquired;try{acquired=mutex.WaitOne(0);}catch(AbandonedMutexException){acquired=true;}
        if(!acquired){NativeWindowTools.ShowExisting();return 0;}
        try
        {
            using var lifecycle=new CodexLifecycle();
            if(args.Contains("--watch")&&!demo)
            {
                using var follow=new FollowContext(lifecycle,true,false,reportPath);
                Application.Run(follow);return 0;
            }
            bool attached=demo&&testParent==0 || (testParent>0
                ?lifecycle.AttachTestProcessAsync(testParent,CancellationToken.None)
                :lifecycle.AttachOrLaunchAsync(!args.Contains("--attach-only"),CancellationToken.None)).GetAwaiter().GetResult();
            if(!attached){MessageBox.Show(lifecycle.Status+"\n请先手动打开 Codex，再启动本程序。","Quota Float",MessageBoxButtons.OK,MessageBoxIcon.Information);return 2;}
            if(!demo)
            {
                using var follow=new FollowContext(lifecycle,FollowStartup.Enabled,false,reportPath);
                Application.Run(follow);return 0;
            }
            var store=new PreferenceStore(!demo);var preferences=store.Load();
            if(demo)
            {
                preferences.X=160;preferences.Y=160;preferences.HoverExpand=false;
                if(Value("--mode") is "full" or "mini" or "orb")preferences.Mode=Value("--mode")!;
                if(int.TryParse(Value("--scale"),out var demoScale))preferences.FullScalePercent=Math.Clamp(demoScale,100,180);
            }
            using var form=new QuotaForm(preferences,store,lifecycle,demo,reportPath);
            if(demo&&int.TryParse(Value("--exit-after"),out var seconds)&&seconds>0)form.ExitAfter(TimeSpan.FromSeconds(Math.Min(seconds,3600)));
            Application.Run(form);return 0;
        }
        finally{mutex.ReleaseMutex();}
    }
}
