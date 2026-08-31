using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Text.Json;
using Microsoft.Win32;

namespace QuotaFloat;

public sealed class QuotaForm : Form
{
    Preferences prefs;
    readonly PreferenceStore store;
    readonly CodexLifecycle lifecycle;
    readonly QuotaClient client=new();
    readonly CancellationTokenSource cancellation=new();
    readonly System.Windows.Forms.Timer clock=new(){Interval=30000};
    readonly System.Windows.Forms.Timer hover=new(){Interval=250};
    System.Windows.Forms.Timer? exitTimer;
    readonly NotifyIcon tray;
    readonly Icon appIcon;
    readonly ToolTip tips=new();
    readonly bool demo;
    readonly string? report;
    QuotaSnapshot? snapshot;
    string fetchStatus="loading";
    bool busy,closing,dialogOpen;
    bool suppressHoverUntilLeave;
    bool nativeResizeInProgress,interactionFinished,customMoveInProgress;
    NativeWindowTools.ResizeHitTest lastHitTest;
    string lastMode="";
    SettingsForm? settingsDialog;
    string? hoverReturn;
    DateTimeOffset nextRefresh=DateTimeOffset.MinValue,lastAttempt=DateTimeOffset.MinValue;
    int failures;
    int lastRisk;
    long observedSession;
    Color surface,ink,secondary,line,healthy,caution,critical;
    float ScaleFactor=>DeviceDpi/96f;
    int FullScalePercent=>Math.Clamp(prefs.FullScalePercent,100,180);
    float PreferredUiScale=>ScaleFactor*(prefs.Mode=="full"?FullScalePercent/100f:1f);
    float UiScale
    {
        get
        {
            if(prefs.Mode!="full"||ClientSize.Width<=0||ClientSize.Height<=0)return PreferredUiScale;
            // A border resize scales the whole logical canvas, keeping text and controls legible.
            float resized=Math.Min(ClientSize.Width/(float)BaseFullWidth,ClientSize.Height/(float)BaseFullHeight);
            return Math.Clamp(resized,ScaleFactor,ScaleFactor*1.8f);
        }
    }
    int S(float n)=>(int)Math.Round(n*UiScale);
    int BaseFullWidth=>prefs.Comfortable?330:306;
    int BaseFullHeight=>286+NoticeHeight;
    int LogicalWidth=>prefs.Mode=="full"?BaseFullWidth:prefs.Mode=="mini"?216:80;
    int LogicalHeight=>prefs.Mode=="full"?BaseFullHeight:prefs.Mode=="mini"?76:80;
    bool CanShow=>snapshot is not null && fetchStatus is not ("signedout" or "format" or "ratelimit") && DateTimeOffset.UtcNow-snapshot.UpdatedAt<TimeSpan.FromMinutes(30);
    string Status=>busy?"loading":snapshot is not null&&DateTimeOffset.UtcNow-snapshot.UpdatedAt>=TimeSpan.FromMinutes(30)?"expired":fetchStatus=="ok"&&snapshot is not null&&DateTimeOffset.UtcNow-snapshot.UpdatedAt>TimeSpan.FromMinutes(10)?"stale":fetchStatus;
    int NoticeHeight=>CanShow&&Status is not ("ok" or "loading")?30:0;
    string T(string zh,string en)=>prefs.Language=="en"?en:zh;
    public QuotaForm(Preferences prefs,PreferenceStore store,CodexLifecycle lifecycle,bool demo,string? report,bool coordinatedLifecycle=false)
    {
        this.prefs=prefs;this.store=store;this.lifecycle=lifecycle;this.demo=demo;this.report=report;
        Text=demo?"Quota Float · DEMO":"Quota Float";Name="QuotaFloatWindow";AccessibleName="quota-float-window";
        FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=demo;StartPosition=FormStartPosition.Manual;AutoScaleMode=AutoScaleMode.None;
        SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
        appIcon=MakeIcon();Icon=appIcon;tray=new NotifyIcon{Icon=appIcon,Text="Quota Float",Visible=true};
        tray.MouseDoubleClick+=(_,_)=>RestoreWidget();
        if(!coordinatedLifecycle)lifecycle.DesktopExited+=DesktopExited;
        clock.Tick+=async(_,_)=>{if(closing)return;Invalidate();if(prefs.AutoRefresh&&DateTimeOffset.UtcNow>=nextRefresh)await RefreshQuotaAsync(false);};
        hover.Tick+=(_,_)=>{if(hoverReturn is not null&&!dialogOpen&&!Bounds.Contains(Cursor.Position)){prefs.Mode=hoverReturn;hoverReturn=null;hover.Stop();Rebuild();}};
        Shown+=async(_,_)=>{if(!demo&&!coordinatedLifecycle&&lifecycle.DesktopProcessId is null){Close();return;}ClampPosition();clock.Start();WriteReport("started");await RefreshQuotaAsync(true);};
        FormClosed+=(_,_)=>Cleanup();
        FormClosing+=(_,e)=>{if(!demo)RuntimeDiagnostics.Record("widget-closing-"+e.CloseReason);};
        DpiChanged+=(_,_)=>Rebuild();
        SystemEvents.UserPreferenceChanged+=SystemPreferenceChanged;
        Rebuild();
        var work=Screen.PrimaryScreen!.WorkingArea;
        Location=prefs.X is int x&&prefs.Y is int y?new Point(x,y):new Point(work.Right-Width-24,work.Top+80);
    }
    void SystemPreferenceChanged(object sender,UserPreferenceChangedEventArgs e)
    {
        if(prefs.Theme!="system"||closing||!IsHandleCreated)return;
        try{BeginInvoke((Action)Rebuild);}catch(InvalidOperationException){} // The handle can close between event and dispatch.
    }
    void DesktopExited(object? sender,EventArgs e)
    {
        if(closing)return;
        if(IsHandleCreated)try{BeginInvoke((Action)(()=>{WriteReport("parent-exited");if(!demo)RuntimeDiagnostics.Record("desktop-exited");Close();}));}catch(InvalidOperationException){}
        else Shown+=(_,_)=>Close();
    }
    public void ExitAfter(TimeSpan time){exitTimer=new(){Interval=(int)time.TotalMilliseconds};exitTimer.Tick+=(_,_)=>Close();exitTimer.Start();}
    static Icon MakeIcon()
    {
        using var bitmap=new Bitmap(32,32);using(var g=Graphics.FromImage(bitmap))
        {g.Clear(Color.FromArgb(32,57,47));using var p=new Pen(Color.FromArgb(163,222,194),3);g.DrawLines(p,new Point[]{new(7,8),new(13,16),new(7,24)});g.DrawLine(p,17,24,25,24);}
        var handle=bitmap.GetHicon();try{return (Icon)Icon.FromHandle(handle).Clone();}finally{DestroyIcon(handle);}
    }
    [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool DestroyIcon(IntPtr handle);
    void Palette()
    {
        bool light=prefs.Theme=="light";
        if(prefs.Theme=="system")try{using var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");light=key?.GetValue("AppsUseLightTheme") is int n&&n!=0;}catch{light=false;}
        surface=ColorTranslator.FromHtml(light?"#F3F6F4":"#1C2630");ink=ColorTranslator.FromHtml(light?"#1C3037":"#EDF4F8");secondary=ColorTranslator.FromHtml(light?"#54696D":"#ABBDC5");line=ColorTranslator.FromHtml(light?"#CBD6D7":"#435461");
        healthy=ColorTranslator.FromHtml(light?"#237453":"#91CEB6");caution=ColorTranslator.FromHtml(light?"#805815":"#DBB77F");critical=ColorTranslator.FromHtml(light?"#AB3D46":"#E7A2A0");
        if(prefs.Skin=="soft")surface=ColorTranslator.FromHtml(light?"#EDF4ED":"#20322E");
        if(prefs.Skin=="terminal"){surface=ColorTranslator.FromHtml(light?"#F1F0DF":"#202522");healthy=ColorTranslator.FromHtml(light?"#497529":"#B2D992");}
        BackColor=surface;ForeColor=ink;
    }
    void Rebuild()
    {
        if(closing||IsDisposed||Disposing||nativeResizeInProgress)return;
        SuspendLayout();Palette();TopMost=prefs.Topmost&&!dialogOpen;
        foreach(Control control in Controls.Cast<Control>().ToArray()){Controls.Remove(control);control.Dispose();}
        bool enteringOrb=lastMode!="orb"&&prefs.Mode=="orb";lastMode=prefs.Mode;
        if(prefs.Mode=="full")
        {
            var area=Screen.FromRectangle(Bounds).WorkingArea;
            int fittingScale=(int)Math.Floor(100*Math.Min((area.Width-8)/(BaseFullWidth*ScaleFactor),(area.Height-8)/(BaseFullHeight*ScaleFactor)));
            prefs.FullScalePercent=Math.Clamp(FullScalePercent,100,Math.Clamp(fittingScale,100,180));
            ClientSize=new Size((int)Math.Round(BaseFullWidth*PreferredUiScale),(int)Math.Round(BaseFullHeight*PreferredUiScale));
        }
        else ClientSize=new Size((int)Math.Round(LogicalWidth*ScaleFactor),(int)Math.Round(LogicalHeight*ScaleFactor));
        int h=LogicalHeight;
        UpdateRegion();
        if(prefs.Mode=="full")
        {
            ButtonAt("pin-button",prefs.Topmost?"●":"○",LogicalWidth-88,10,24,24,T("置顶","Always on top"),()=>{prefs.Topmost=!prefs.Topmost;Save();Rebuild();});
            ButtonAt("orb-button","◉",LogicalWidth-115,10,24,24,T("切换为小球；拖到屏幕边缘也会自动切换","Switch to orb; dragging to a screen edge also switches"),()=>SetMode("orb"));
            ButtonAt("collapse-button","−",LogicalWidth-61,10,24,24,T("收起","Collapse"),()=>SetMode("mini"));
            ButtonAt("settings-button","⚙",LogicalWidth-34,10,24,24,T("设置","Settings"),OpenSettings);
            ButtonAt("refresh-button","↻",LogicalWidth-34,h-30,24,24,T("刷新","Refresh"),async()=>await RefreshQuotaAsync(true)).Enabled=!busy;
            if(CanShow)ButtonAt("credits-button","›",LogicalWidth-34,207+NoticeHeight,24,23,T("重置机会详情","Reset credit details"),ShowCredits);
            else if(!busy)ButtonAt("retry-button",T("重新读取","Retry"),LogicalWidth/2-48,175,96,28,T("重新读取额度","Read quota again"),async()=>await RefreshQuotaAsync(true));
        }
        else ButtonAt("expand-button","›",LogicalWidth-25,prefs.Mode=="orb"?3:6,20,20,T("展开","Expand"),()=>SetMode("full"));
        if(IsHandleCreated)NativeWindowTools.ClickThrough(this,prefs.ClickThrough&&!dialogOpen);
        ArrangeControls();BuildTray();ClampPosition();ResumeLayout();Invalidate();
        if(enteringOrb)suppressHoverUntilLeave=Bounds.Contains(Cursor.Position);
        AccessibleDescription=$"{StatusText()} 5h {Percent(snapshot?.Short)} Weekly {Percent(snapshot?.Weekly)}";
        tips.SetToolTip(this,prefs.Mode=="orb"?AccessibleDescription:null);
    }
    Button ButtonAt(string id,string text,int x,int y,int width,int height,string tip,Action action)
    {
        var button=new Button{Name=id,AccessibleName=id,AccessibleDescription=tip,Text=text,FlatStyle=FlatStyle.Flat,BackColor=surface,ForeColor=secondary,TabStop=true,Bounds=new Rectangle(S(x),S(y),S(width),S(height)),Font=new Font("Segoe UI",S(12),GraphicsUnit.Pixel)};
        button.FlatAppearance.BorderSize=0;button.FlatAppearance.MouseOverBackColor=line;button.Click+=(_,_)=>action();Controls.Add(button);tips.SetToolTip(button,tip);return button;
    }
    void ArrangeControls()
    {
        if(prefs.Mode!="full")return;
        int w=LogicalWidth,h=LogicalHeight;
        void Move(string id,int x,int y,int width,int height)
        {
            if(Controls.Find(id,true).FirstOrDefault() is Control control)
            {
                control.Bounds=new Rectangle(S(x),S(y),S(width),S(height));
                if(Math.Abs(control.Font.Size-S(12))>.1f){var old=control.Font;control.Font=new Font("Segoe UI",S(12),GraphicsUnit.Pixel);old.Dispose();}
            }
        }
        Move("orb-button",w-115,10,24,24);Move("pin-button",w-88,10,24,24);Move("collapse-button",w-61,10,24,24);Move("settings-button",w-34,10,24,24);
        Move("refresh-button",w-34,h-30,24,24);Move("credits-button",w-34,207+NoticeHeight,24,23);
    }
    void UpdateRegion()
    {
        if(Width<=0||Height<=0)return;
        using var path=Rounded(new RectangleF(0,0,Width-1,Height-1),S(prefs.Skin=="terminal"?8:17));var previous=Region;Region=new Region(path);previous?.Dispose();
    }
    void BuildTray()
    {
        var old=tray.ContextMenuStrip;var menu=new ContextMenuStrip();
        void Item(string label,Action action,bool? check=null){var item=new ToolStripMenuItem(label){Checked=check??false};item.Click+=(_,_)=>action();menu.Items.Add(item);}
        Item(T("显示 / 解锁浮窗","Show / unlock widget"),RestoreWidget);
        Item(T("隐藏浮窗","Hide widget"),Hide);
        Item(T("立即刷新","Refresh now"),async()=>await RefreshQuotaAsync(true));
        menu.Items.Add(new ToolStripSeparator());
        Item(T("一窗看全","Full window"),()=>SetMode("full"),prefs.Mode=="full");Item(T("双额度胶囊","Dual quota pill"),()=>SetMode("mini"),prefs.Mode=="mini");Item(T("双额度小球","Dual quota orb"),()=>SetMode("orb"),prefs.Mode=="orb");
        Item(T("始终置顶","Always on top"),()=>{prefs.Topmost=!prefs.Topmost;Save();Rebuild();},prefs.Topmost);
        Item(T("鼠标穿透（托盘解锁）","Click-through (unlock from tray)"),()=>{prefs.ClickThrough=!prefs.ClickThrough;Rebuild();},prefs.ClickThrough);
        Item(T("主题与设置","Themes and settings"),OpenSettings);
        Item(T("找回窗口","Recover window position"),()=>{var area=Screen.PrimaryScreen!.WorkingArea;Location=new Point(area.Right-Width-24,area.Top+80);RestoreWidget();Save();});
        menu.Items.Add(new ToolStripSeparator());
        Item(T("退出浮窗，不关闭 Codex","Quit widget; keep Codex open"),Close);
        tray.ContextMenuStrip=menu;old?.Dispose();
    }
    void SetMode(string mode){hover.Stop();hoverReturn=null;prefs.Mode=mode;if(mode!="full")prefs.KeepExpanded=false;Rebuild();suppressHoverUntilLeave=mode=="orb"&&Bounds.Contains(Cursor.Position);Save();}
    void RestoreWidget()
    {
        if(settingsDialog is {IsDisposed:false} dialog){dialog.Activate();dialog.BringToFront();return;}
        if(dialogOpen)return;
        prefs.ClickThrough=false;Show();Rebuild();Activate();
    }
    void Save(){prefs.X=Left;prefs.Y=Top;if(!store.Save(prefs)&&!closing)tray.ShowBalloonTip(4000,"Quota Float",store.Warning??"Preferences could not be saved.",ToolTipIcon.Warning);}
    void ClampPosition()
    {
        if(Width<=0)return;var area=Screen.FromRectangle(Bounds).WorkingArea;
        Location=new Point(Math.Clamp(Left,area.Left+4,Math.Max(area.Left+4,area.Right-Width-4)),Math.Clamp(Top,area.Top+4,Math.Max(area.Top+4,area.Bottom-Height-4)));
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);if(e.Button!=MouseButtons.Left||prefs.ClickThrough)return;
        if(e.Y<S(44)||prefs.Mode!="full")
        {
            nativeResizeInProgress=false;interactionFinished=false;customMoveInProgress=true;tips.Active=false;
            try{NativeWindowTools.Drag(this);}finally{customMoveInProgress=false;if(!closing&&!IsDisposed)tips.Active=true;}
            FinishNativeMove();
        }
    }
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);if(prefs is not null&&!closing&&!Disposing){UpdateRegion();if(prefs.Mode=="full")ArrangeControls();Invalidate();}
    }

    public static NativeWindowTools.SizeLimits GetFullSizeLimits(Rectangle workingArea,float dpiScale,int scalePercent,bool comfortable=false,int noticeHeight=0)
    {
        dpiScale=Math.Max(.5f,dpiScale);scalePercent=Math.Clamp(scalePercent,100,180);int logicalWidth=comfortable?330:306,logicalHeight=286+Math.Max(0,noticeHeight);
        var minimum=new Size((int)Math.Round(logicalWidth*dpiScale),(int)Math.Round(logicalHeight*dpiScale));
        var scaleMaximum=new Size((int)Math.Round(logicalWidth*1.8f*dpiScale),(int)Math.Round(logicalHeight*1.8f*dpiScale));
        var areaMaximum=new Size(Math.Max(1,workingArea.Width-8),Math.Max(1,workingArea.Height-8));
        var maximum=new Size(Math.Max(minimum.Width,Math.Min(scaleMaximum.Width,areaMaximum.Width)),Math.Max(minimum.Height,Math.Min(scaleMaximum.Height,areaMaximum.Height)));
        return new NativeWindowTools.SizeLimits(minimum,maximum);
    }

    void FinishNativeMove()
    {
        if(interactionFinished||closing||IsDisposed)return;interactionFinished=true;
        if(prefs.Mode=="full")
        {
            var area=Screen.FromRectangle(Bounds).WorkingArea;
            if(!nativeResizeInProgress&&prefs.EdgeCollapse&&NativeWindowTools.ShouldConvertToOrb(Bounds,area))
            {
                bool left=Bounds.Left<=area.Left,right=Bounds.Right>=area.Right,top=Bounds.Top<=area.Top,bottom=Bounds.Bottom>=area.Bottom;
                SetMode("orb");
                if(left)Left=area.Left+4;else if(right)Left=area.Right-Width-4;
                if(top)Top=area.Top+4;else if(bottom)Top=area.Bottom-Height-4;
                ClampPosition();Save();return;
            }
            if(nativeResizeInProgress)
            {
                prefs.FullScalePercent=Math.Clamp((int)Math.Round(100*UiScale/ScaleFactor),100,180);
                nativeResizeInProgress=false;Rebuild();Save();return;
            }
            ClampPosition();
            if(prefs.Snap)
            {
                int threshold=S(24);
                if(Left-area.Left<threshold)Left=area.Left+4;if(area.Right-Right<threshold)Left=area.Right-Width-4;
                if(Top-area.Top<threshold)Top=area.Top+4;if(area.Bottom-Bottom<threshold)Top=area.Bottom-Height-4;
            }
        }
        else ClampPosition();
        Save();
    }
    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);if(suppressHoverUntilLeave)return;if(prefs.Mode!="full"&&prefs.HoverExpand&&!prefs.ClickThrough&&!dialogOpen){hoverReturn=prefs.Mode;prefs.Mode="full";Rebuild();if(prefs.KeepExpanded)hoverReturn=null;else hover.Start();}
    }
    protected override void OnMouseLeave(EventArgs e){if(!Bounds.Contains(Cursor.Position))suppressHoverUntilLeave=false;base.OnMouseLeave(e);}
    protected override void WndProc(ref Message m)
    {
        const int wmNcHitTest=0x84,wmGetMinMaxInfo=0x24,wmSizing=0x214,wmEnterSizeMove=0x231,wmExitSizeMove=0x232;
        if(m.Msg==NativeWindowTools.RestoreMessage){RestoreWidget();return;}
        if(m.Msg==wmNcHitTest)
        {
            var point=new Point(unchecked((short)m.LParam.ToInt64()),unchecked((short)(m.LParam.ToInt64()>>16)));
            var hit=NativeWindowTools.GetResizeHitTest(Bounds,point,(int)Math.Round(8*ScaleFactor),prefs.Mode=="full"&&!dialogOpen&&!prefs.ClickThrough);
            if(!customMoveInProgress)lastHitTest=hit;
            m.Result=new IntPtr(hit switch{NativeWindowTools.ResizeHitTest.Left=>10,NativeWindowTools.ResizeHitTest.Right=>11,NativeWindowTools.ResizeHitTest.Top=>12,NativeWindowTools.ResizeHitTest.TopLeft=>13,NativeWindowTools.ResizeHitTest.TopRight=>14,NativeWindowTools.ResizeHitTest.Bottom=>15,NativeWindowTools.ResizeHitTest.BottomLeft=>16,NativeWindowTools.ResizeHitTest.BottomRight=>17,_=>1});return;
        }
        if(m.Msg==wmGetMinMaxInfo&&prefs.Mode=="full"&&m.LParam!=IntPtr.Zero)
        {
            var info=System.Runtime.InteropServices.Marshal.PtrToStructure<MinMaxInfo>(m.LParam);var limits=GetFullSizeLimits(Screen.FromHandle(Handle).WorkingArea,ScaleFactor,FullScalePercent,prefs.Comfortable,NoticeHeight);
            info.ptMinTrackSize=new Point(limits.Minimum.Width,limits.Minimum.Height);info.ptMaxTrackSize=new Point(limits.Maximum.Width,limits.Maximum.Height);System.Runtime.InteropServices.Marshal.StructureToPtr(info,m.LParam,false);m.Result=IntPtr.Zero;return;
        }
        if(m.Msg==wmSizing&&prefs.Mode=="full"&&m.LParam!=IntPtr.Zero)
        {
            nativeResizeInProgress=true;
            var rect=System.Runtime.InteropServices.Marshal.PtrToStructure<SizingRect>(m.LParam);var limits=GetFullSizeLimits(Screen.FromHandle(Handle).WorkingArea,ScaleFactor,FullScalePercent,prefs.Comfortable,NoticeHeight);
            var requested=new Size(rect.Right-rect.Left,rect.Bottom-rect.Top);
            if(m.WParam.ToInt32() is 3 or 6)requested.Width=(int)Math.Round(requested.Height*(double)BaseFullWidth/BaseFullHeight);
            var size=NativeWindowTools.ConstrainProportionalSize(requested,limits,(double)BaseFullWidth/BaseFullHeight);
            switch(m.WParam.ToInt32())
            {
                case 1:rect.Left=rect.Right-size.Width;rect.Bottom=rect.Top+size.Height;break;
                case 2:rect.Right=rect.Left+size.Width;rect.Bottom=rect.Top+size.Height;break;
                case 3:rect.Top=rect.Bottom-size.Height;rect.Right=rect.Left+size.Width;break;
                case 4:rect.Left=rect.Right-size.Width;rect.Top=rect.Bottom-size.Height;break;
                case 5:rect.Right=rect.Left+size.Width;rect.Top=rect.Bottom-size.Height;break;
                case 6:rect.Right=rect.Left+size.Width;rect.Bottom=rect.Top+size.Height;break;
                case 7:rect.Left=rect.Right-size.Width;rect.Bottom=rect.Top+size.Height;break;
                case 8:rect.Right=rect.Left+size.Width;rect.Bottom=rect.Top+size.Height;break;
            }
            System.Runtime.InteropServices.Marshal.StructureToPtr(rect,m.LParam,false);m.Result=new IntPtr(1);return;
        }
        if(m.Msg==wmEnterSizeMove){if(!customMoveInProgress)nativeResizeInProgress=lastHitTest is NativeWindowTools.ResizeHitTest.Left or NativeWindowTools.ResizeHitTest.Right or NativeWindowTools.ResizeHitTest.Top or NativeWindowTools.ResizeHitTest.TopLeft or NativeWindowTools.ResizeHitTest.TopRight or NativeWindowTools.ResizeHitTest.Bottom or NativeWindowTools.ResizeHitTest.BottomLeft or NativeWindowTools.ResizeHitTest.BottomRight;interactionFinished=false;}
        base.WndProc(ref m);
        if(m.Msg==wmExitSizeMove)FinishNativeMove();
    }
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct MinMaxInfo{public Point ptReserved,ptMaxSize,ptMaxPosition,ptMinTrackSize,ptMaxTrackSize;}
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct SizingRect{public int Left,Top,Right,Bottom;}
    async Task RefreshQuotaAsync(bool manual)
    {
        if(busy||closing)return;
        var now=DateTimeOffset.UtcNow;
        if(manual&&now-lastAttempt<TimeSpan.FromSeconds(10))return;
        if(fetchStatus=="ratelimit"&&now<nextRefresh)return;
        lastAttempt=now;busy=true;Rebuild();
        try
        {
            QuotaResult result;
            if(demo)result=new(new("PRO",new(72,now.AddHours(2.3),18000),new(38,now.AddDays(3),604800),2,[now.AddDays(15),now.AddDays(30)],now),"ok");
            else result=await client.FetchAsync(cancellation.Token);
            if(closing)return;
            if(!demo&&observedSession!=client.SessionGeneration){snapshot=null;lastRisk=0;observedSession=client.SessionGeneration;}
            fetchStatus=result.Status;
            if(result.Snapshot is {} fresh)
            {
                snapshot=fresh;failures=0;
                var nearReset=new[]{fresh.Short?.ResetAt,fresh.Weekly?.ResetAt}.Any(x=>x is not null&&x>now&&x-now<TimeSpan.FromMinutes(15));
                nextRefresh=now.AddSeconds(nearReset?60:prefs.RefreshSeconds);
                int risk=(fresh.Short?.Remaining<prefs.Critical||fresh.Weekly?.Remaining<prefs.Critical)?2:(fresh.Short?.Remaining<prefs.Warning||fresh.Weekly?.Remaining<prefs.Warning)?1:0;
                if(prefs.Notify&&risk>lastRisk)tray.ShowBalloonTip(5000,"Codex Quota",T("至少一个额度窗口偏低，请查看浮窗。","At least one quota window is low. Check the widget."),ToolTipIcon.Warning);
                lastRisk=risk;
            }
            else
            {
                if(result.Status is "signedout" or "format")snapshot=null;
                failures++;double seconds=Math.Min(1800,30*Math.Pow(2,Math.Min(failures-1,6)));
                if(result.RetryAfter is {} retry)seconds=Math.Max(seconds,Math.Clamp(retry.TotalSeconds,30,86400));
                nextRefresh=DateTimeOffset.UtcNow.AddSeconds(seconds);
            }
            WriteReport("refreshed");
        }
        catch(OperationCanceledException)when(cancellation.IsCancellationRequested){}
        finally{busy=false;if(!closing)Rebuild();}
    }
    string StatusText()=>Status switch
    {
        "loading"=>T("读取中","Reading"),"ok"=>T("已同步","Synced"),"partial"=>T("信息不完整","Partial data"),"expired"=>T("快照已过期","Snapshot expired"),"stale"=>T("旧数据 · 待刷新","Stale snapshot"),"offline"=>T("离线 · 上次快照","Offline · cached"),"signedout"=>T("请登录 Codex","Sign in to Codex"),"sessionchanged"=>T("账户已变化，等待重读","Account changed; retrying"),"ratelimit"=>T("请求受限 · 稍后重试","Rate limited"),"format"=>T("数据格式无法识别","Unknown response format"),_=>T("服务暂不可用","Service unavailable")
    };
    string Percent(QuotaWindow? window)=>CanShow&&window is not null?window.Remaining.ToString("0.#")+"%":"--";
    Color Risk(QuotaWindow? window)=>window is null?secondary:window.Remaining<prefs.Critical?critical:window.Remaining<prefs.Warning?caution:healthy;
    string Countdown(QuotaWindow? window)
    {
        if(window?.ResetAt is not {} date)return T("重置时间未知","Reset time unknown");var span=date-DateTimeOffset.UtcNow;
        if(span<=TimeSpan.Zero)return T("已到重置时间，待确认","Reset due; awaiting sync");
        return span.TotalDays>=1?T($"{(int)span.TotalDays} 天 {span.Hours}h 后重置",$"Resets in {(int)span.TotalDays}d {span.Hours}h"):T($"{(int)span.TotalHours}h {span.Minutes}m 后重置",$"Resets in {(int)span.TotalHours}h {span.Minutes}m");
    }
    static GraphicsPath Rounded(RectangleF rect,float radius)
    {
        var path=new GraphicsPath();float d=radius*2;path.AddArc(rect.X,rect.Y,d,d,180,90);path.AddArc(rect.Right-d,rect.Y,d,d,270,90);path.AddArc(rect.Right-d,rect.Bottom-d,d,d,0,90);path.AddArc(rect.X,rect.Bottom-d,d,d,90,90);path.CloseFigure();return path;
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var g=e.Graphics;g.ScaleTransform(UiScale,UiScale);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        int w=LogicalWidth,h=LogicalHeight;
        using var border=new Pen(line);
        using(var path=Rounded(new RectangleF(.75f,.75f,w-2,h-2),prefs.Skin=="terminal"?8:17))g.DrawPath(border,path);
        using(var inset=new Pen(Color.FromArgb(26,ink)))using(var path=Rounded(new RectangleF(2,2,w-4.5f,h-4.5f),prefs.Skin=="terminal"?6:15))g.DrawPath(inset,path);
        void TextAt(string text,float x,float y,float size,Color color,float width=200,bool bold=false,StringAlignment align=StringAlignment.Near)
        {
            using var font=new Font(prefs.Skin=="terminal"?"Consolas":"Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel);using var brush=new SolidBrush(color);using var format=new StringFormat{Alignment=align,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap};g.DrawString(text,font,brush,new RectangleF(x,y,width,size*1.6f),format);
        }
        if(prefs.Mode!="full")
        {
            void SmallBar(QuotaWindow? window,float x,float y,float width)
            {
                using var track=new Pen(line,3){StartCap=LineCap.Round,EndCap=LineCap.Round};g.DrawLine(track,x,y,x+width,y);
                if(CanShow&&window is {Remaining:>0}){using var fill=new Pen(Risk(window),3){StartCap=LineCap.Round,EndCap=LineCap.Round};g.DrawLine(fill,x,y,x+width*(float)window.Remaining/100,y);}
            }
            if(prefs.Mode=="mini")
            {
                TextAt("›_",12,23,17,healthy,30,true);TextAt(T("5h 剩余","5h left"),43,9,9,secondary,68);TextAt(T("周剩余","Week left"),123,9,9,secondary,65);
                TextAt(Percent(snapshot?.Short),43,22,21,Risk(snapshot?.Short),74,true);TextAt(Percent(snapshot?.Weekly),123,22,21,Risk(snapshot?.Weekly),75,true);
                SmallBar(snapshot?.Short,46,50,60);SmallBar(snapshot?.Weekly,126,50,60);
                TextAt((demo?"DEMO · ":"")+StatusText(),43,59,8,Status=="ok"?secondary:caution,162);
            }
            else
            {
                using var dot=new SolidBrush(Status=="ok"?healthy:caution);g.FillEllipse(dot,10,11,4,4);
                TextAt(demo?"DEMO":T("额度","QUOTA"),18,7,8,secondary,39);
                TextAt("5h",10,29,9,secondary,20);TextAt(Percent(snapshot?.Short),28,24,16,Risk(snapshot?.Short),43,true,StringAlignment.Far);SmallBar(snapshot?.Short,11,45,58);
                TextAt(T("周","Wk"),10,53,9,secondary,20);TextAt(Percent(snapshot?.Weekly),28,48,16,Risk(snapshot?.Weekly),43,true,StringAlignment.Far);SmallBar(snapshot?.Weekly,11,69,58);
            }
            return;
        }
        TextAt("›_",14,12,16,healthy,26,true);TextAt("Codex",39,13,13,ink,55,true);TextAt(snapshot?.Plan??"—",98,15,9,secondary,55);TextAt(demo?"DEMO":"LIVE",154,16,8,secondary,50);g.DrawLine(border,0,43,w,43);
        if(CanShow)
        {
            int offset=NoticeHeight;
            if(offset>0)TextAt(StatusText()+T(" · 请留意更新时间"," · check data age"),15,49,10,caution,w-30);
            void Row(QuotaWindow? window,string label,int y)
            {
                TextAt(label,15,y+4,prefs.FontSize,ink,w-115);TextAt(Percent(window),w-100,y-4,26,Risk(window),85,true,StringAlignment.Far);
                using var track=new Pen(line,4){StartCap=LineCap.Round,EndCap=LineCap.Round};g.DrawLine(track,17,y+35,w-17,y+35);
                if(window is {Remaining:>0}){using var fill=new Pen(Risk(window),4){StartCap=LineCap.Round,EndCap=LineCap.Round};g.DrawLine(fill,17,y+35,17+(float)((w-34)*window.Remaining/100),y+35);}
                else if(window is not null){}
                else{using var erase=new Pen(surface,3);for(int x=15;x<w-15;x+=8)g.DrawLine(erase,x,y+32,x,y+38);}
                TextAt(Countdown(window),15,y+44,9,secondary,w-120);TextAt(window?.ResetAt?.LocalDateTime.ToString("M/d HH:mm")??"",w-104,y+44,9,secondary,89,false,StringAlignment.Far);
            }
            Row(snapshot!.Short,T("5 小时剩余","5-hour remaining"),58+offset);Row(snapshot.Weekly,T("每周剩余","Weekly remaining"),132+offset);
            g.DrawLine(border,15,205+offset,w-15,205+offset);
            TextAt(T("重置机会 ","Reset credits ")+(snapshot.Credits?.ToString()??T("未知","Unknown")),15,216+offset,10,ink,140);
            var expiry=snapshot.Expiries.FirstOrDefault();string expiryText=snapshot.Credits==0?T("暂无可用","None available"):expiry==default?T("到期未知","Expiry unknown"):expiry<=DateTimeOffset.UtcNow?T("到期待核对","Expiry needs sync"):T("到期 ","Expires ")+expiry.LocalDateTime.ToString("M/d");
            TextAt(expiryText,w-137,216+offset,9,secondary,104,false,StringAlignment.Far);
        }
        else
        {
            TextAt(StatusText(),20,81,16,caution,w-40,true,StringAlignment.Center);
            string description=Status=="signedout"?T("只读取本地 Codex 登录状态，不代替登录。","Reads local Codex auth; never signs in for you."):Status=="ratelimit"?T("按服务端要求退避，不重复轰炸接口。","Waiting before retry; no repeated requests."):Status=="expired"?T("旧快照不再当作可用额度显示。","Old data is not shown as current quota."):T("没有可靠数据时显示未知，不会填成 0%。","Unknown data never becomes 0% quota.");
            TextAt(description,15,120,10,secondary,w-30,false,StringAlignment.Center);
            if(Status=="ratelimit")TextAt(T("下次重试 ","Retry after ")+nextRefresh.LocalDateTime.ToString("HH:mm:ss"),20,144,10,secondary,w-40,false,StringAlignment.Center);
        }
        g.DrawLine(border,0,h-37,w,h-37);TextAt((demo?"DEMO · ":"")+StatusText(),15,h-24,9,Status is "ok"?healthy:caution,w-128);
        string age=snapshot is null?T("无快照","No data"):T($"{Math.Max(0,(int)(DateTimeOffset.UtcNow-snapshot.UpdatedAt).TotalMinutes)} 分钟前",$"{Math.Max(0,(int)(DateTimeOffset.UtcNow-snapshot.UpdatedAt).TotalMinutes)}m ago");TextAt(age,w-110,h-24,9,secondary,70,false,StringAlignment.Far);
    }
    void ShowCredits()
    {
        if(snapshot is null||dialogOpen)return;dialogOpen=true;TopMost=false;
        try
        {
            string lines=snapshot.Expiries.Length==0?T("到期时间未提供。","Expiry dates were not provided."):string.Join(Environment.NewLine,snapshot.Expiries.Select((x,i)=>$"{i+1}. {x.LocalDateTime:yyyy-MM-dd HH:mm} {(x<=DateTimeOffset.UtcNow?T("到期，待服务端确认","expired; confirm with server"):"")}"));
            MessageBox.Show(this,T("服务端快照中的重置机会：","Reset credits in source snapshot: ")+(snapshot.Credits?.ToString()??"?")+Environment.NewLine+lines+Environment.NewLine+T("只读显示，不会兑换或购买。","Read-only. No redemption or purchase."),T("重置机会","Reset credits"),MessageBoxButtons.OK,MessageBoxIcon.Information);
        }finally{dialogOpen=false;Rebuild();}
    }
    void OpenSettings()
    {
        if(dialogOpen){settingsDialog?.Activate();return;}
        RestoreWidget();dialogOpen=true;hover.Stop();TopMost=false;
        try
        {
            using var settings=new SettingsForm(prefs,()=>{Rebuild();Save();},()=>{prefs=new();Rebuild();Save();},demo,lifecycle.Status){TopMost=true};
            settingsDialog=settings;settings.ShowDialog(this);
        }
        finally{settingsDialog=null;dialogOpen=false;Rebuild();if(hoverReturn is not null)hover.Start();}
    }
    void WriteReport(string phase)
    {
        if(report is null)return;
        try{using var p=Process.GetCurrentProcess();File.WriteAllText(report,JsonSerializer.Serialize(new{Phase=phase,Demo=demo,Parent=lifecycle.DesktopProcessId,Status=fetchStatus,Short=snapshot?.Short?.Remaining,Weekly=snapshot?.Weekly?.Remaining,WorkingSetBytes=p.WorkingSet64,CpuSeconds=p.TotalProcessorTime.TotalSeconds,At=DateTimeOffset.UtcNow},new JsonSerializerOptions{WriteIndented=true}));}catch(IOException){}catch(UnauthorizedAccessException){}
    }
    void Cleanup()
    {
        if(closing)return;closing=true;cancellation.Cancel();clock.Stop();clock.Dispose();hover.Dispose();exitTimer?.Dispose();lifecycle.DesktopExited-=DesktopExited;SystemEvents.UserPreferenceChanged-=SystemPreferenceChanged;tray.Visible=false;tray.Dispose();appIcon.Dispose();tips.Dispose();client.Dispose();cancellation.Dispose();if(hoverReturn is not null)prefs.Mode=hoverReturn;Save();
    }
}
