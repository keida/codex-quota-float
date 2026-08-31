namespace QuotaFloat;

// One coordinator owns window lifetime. In watch mode only this message loop and
// its low-frequency presence timer remain when Codex has no desktop windows.
public sealed class FollowContext : ApplicationContext
{
    readonly CodexLifecycle lifecycle;
    bool keepWatching;
    readonly bool demo;
    readonly string? report;
    readonly System.Windows.Forms.Timer timer=new(){Interval=2000};
    readonly NotifyIcon standby;
    readonly CancellationTokenSource cancellation=new();
    readonly DateTimeOffset initialDeadline=DateTimeOffset.UtcNow.AddSeconds(20);
    QuotaForm? widget;
    bool checking,stopping,closingForAbsence,everPresent,disposed;
    int absentSamples;
    public bool WidgetVisible=>widget is {IsDisposed:false,Visible:true};
    public static FollowContext? Current {get;private set;}
    public bool IsWatching=>keepWatching;
    public void SetKeepWatching(bool value){keepWatching=value;standby.Visible=value&&widget is null;}
    public FollowContext(CodexLifecycle lifecycle,bool keepWatching,bool demo,string? report)
    {
        this.lifecycle=lifecycle;this.keepWatching=keepWatching;this.demo=demo;this.report=report;
        Current=this;
        var menu=new ContextMenuStrip();
        menu.Items.Add("停止联动并退出 / Stop following",null,(_,_)=>ExitThread());
        standby=new NotifyIcon{Icon=SystemIcons.Application,Text="Quota Float · 等待 Codex",ContextMenuStrip=menu,Visible=keepWatching};
        timer.Tick+=async(_,_)=>await CheckAsync();timer.Start();
    }
    async Task CheckAsync()
    {
        if(checking||stopping)return;checking=true;
        try
        {
            var presence=await Task.Run(lifecycle.RefreshPresence,cancellation.Token);
            if(stopping)return;
            if(presence.IsPresent)
            {
                everPresent=true;absentSamples=0;timer.Interval=2000;
                if(widget is null)
                {
                    var store=new PreferenceStore(!demo);
                    widget=new QuotaForm(store.Load(),store,lifecycle,demo,report,coordinatedLifecycle:true);
                    widget.FormClosed+=(_,_)=>{widget=null;if(!closingForAbsence&&!stopping)ExitThread();};
                    standby.Visible=false;widget.Show();
                    if(!demo)RuntimeDiagnostics.Record("codex-window-present-widget-opened");
                }
            }
            else if(++absentSamples>=2)
            {
                if(widget is not null)
                {
                    closingForAbsence=true;
                    try{widget.Close();}finally{closingForAbsence=false;}
                    if(!demo)RuntimeDiagnostics.Record("codex-windows-absent-widget-closed");
                }
                if(!keepWatching&&(everPresent||DateTimeOffset.UtcNow>=initialDeadline)){ExitThread();return;}
                timer.Interval=keepWatching?5000:2000;standby.Visible=keepWatching;
            }
        }
        catch(OperationCanceledException)when(stopping){}
        catch(Exception e)when(e is System.ComponentModel.Win32Exception or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            // A failed scan is unknown, not proof that Codex closed.
            if(!demo)RuntimeDiagnostics.Record("presence-scan-unavailable",e);
            absentSamples=0;timer.Interval=5000;
        }
        finally{checking=false;}
    }
    protected override void ExitThreadCore()
    {
        if(stopping)return;stopping=true;timer.Stop();cancellation.Cancel();
        widget?.Close();standby.Visible=false;base.ExitThreadCore();
    }
    protected override void Dispose(bool disposing)
    {
        if(disposing&&!disposed)
        {
            disposed=true;
            if(ReferenceEquals(Current,this))Current=null;
            stopping=true;cancellation.Cancel();timer.Dispose();widget?.Dispose();
            var menu=standby.ContextMenuStrip;standby.Dispose();menu?.Dispose();cancellation.Dispose();
        }
        base.Dispose(disposing);
    }
}
