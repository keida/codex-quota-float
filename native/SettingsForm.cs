namespace QuotaFloat;

public sealed class SettingsForm : Form
{
    readonly Preferences prefs;
    readonly Action changed;
    readonly bool english;
    Color panel,text,muted,border,field,accent;
    readonly List<(Button Button,Panel Page)> pages=[];
    string selectedPage="appearance";
    string T(string zh,string en)=>english?en:zh;
    public SettingsForm(Preferences prefs,Action changed,Action reset,bool demo,string lifecycleStatus)
    {
        this.prefs=prefs;this.changed=()=>{changed();ApplyTheme();};english=prefs.Language=="en";
        UpdatePalette();
        Text=T("Quota Float · 设置","Quota Float · Settings");Name="quota-settings";AccessibleName="quota-settings";
        StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.None;MaximizeBox=false;MinimizeBox=false;ShowInTaskbar=demo;
        AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(584,584);BackColor=panel;ForeColor=text;Font=new Font("Segoe UI",12,GraphicsUnit.Pixel);DoubleBuffered=true;Padding=new Padding(1);
        var header=new Panel{Dock=DockStyle.Top,Height=76,Padding=new Padding(22,17,14,8)};
        header.Controls.Add(new Label{Text=T("偏好设置","Preferences"),AutoSize=true,Location=new Point(22,16),Font=new Font("Segoe UI",19,FontStyle.Bold,GraphicsUnit.Pixel)});
        header.Controls.Add(new Label{Text=T("QUOTA FLOAT  /  本地保存 · 即时生效","QUOTA FLOAT  /  Saved locally · Applied instantly"),AutoSize=true,Location=new Point(23,45),Tag="muted",Font=new Font("Segoe UI",10,GraphicsUnit.Pixel)});
        header.MouseDown+=(_,e)=>{if(e.Button==MouseButtons.Left)NativeWindowTools.Drag(this);};
        var dismiss=new Button{Text="×",Name="settings-close",AccessibleName="settings-close",Dock=DockStyle.Right,Width=30,Font=new Font("Segoe UI",18,GraphicsUnit.Pixel)};dismiss.Click+=(_,_)=>Close();header.Controls.Add(dismiss);
        var footer=new Panel{Dock=DockStyle.Bottom,Height=60,Padding=new Padding(20,12,20,12)};
        header.Paint+=(_,e)=>{using var p=new Pen(border);e.Graphics.DrawLine(p,0,header.Height-1,header.Width,header.Height-1);};
        footer.Paint+=(_,e)=>{using var p=new Pen(border);e.Graphics.DrawLine(p,0,0,footer.Width,0);};
        footer.Controls.Add(new Label{Text=T("无需重启 · 不保存账户凭证","No restart needed · Credentials never saved"),AutoSize=true,Location=new Point(21,24),Tag="muted"});
        var close=new Button{Text=T("完成","Done"),Name="settings-done",AccessibleName="settings-done",Dock=DockStyle.Right,Width=88,DialogResult=DialogResult.OK,Tag="primary"};footer.Controls.Add(close);AcceptButton=close;CancelButton=close;
        var content=new Panel{Dock=DockStyle.Fill,Padding=new Padding(14,0,18,4)};
        var navigation=new FlowLayoutPanel{Dock=DockStyle.Left,Width=122,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(10,8,4,0)};
        Controls.Add(content);Controls.Add(navigation);Controls.Add(footer);Controls.Add(header);
        FlowLayoutPanel Page(string id,string title)
        {
            var flow=new FlowLayoutPanel{Name="page-"+id,AccessibleName="page-"+id,Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(3,2,8,12),Visible=id==selectedPage};content.Controls.Add(flow);
            var nav=new Button{Name="tab-"+id,AccessibleName="tab-"+id,Text=title,Width=104,Height=38,Margin=new Padding(0,0,0,7),TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(8,0,0,0)};
            nav.Click+=(_,_)=>{selectedPage=id;foreach(var entry in pages)entry.Page.Visible=entry.Button==nav;ApplyTheme();};
            pages.Add((nav,flow));navigation.Controls.Add(nav);return flow;
        }
        var appearance=Page("appearance",T("外观","Appearance"));
        Note(appearance,T("一个小窗，完整信息。设置即时保存到本机。","A small window, complete information. Preferences save locally."));
        var modes=new FlowLayoutPanel{Name="mode-buttons",AccessibleName="mode-buttons",Width=388,Height=54,WrapContents=false,Margin=new Padding(0,0,0,8)};
        foreach(var (mode,label) in new[]{("full",T("完整窗 · 可缩放","Full · resizable")),("mini",T("胶囊","Pill")),("orb",T("悬浮球","Orb"))})
        {
            var button=new Button{Name="mode-"+mode+"-button",AccessibleName="mode-"+mode+"-button",Text=label,Width=120,Height=42,Margin=new Padding(0,0,8,0)};
            button.Click+=(_,_)=>{prefs.Mode=mode;this.changed();};modes.Controls.Add(button);
        }
        appearance.Controls.Add(modes);
        Select(appearance,"theme",T("明暗主题","Theme"),["dark","light","system"],[T("深色","Dark"),T("浅色","Light"),T("跟随系统","System")],prefs.Theme,v=>prefs.Theme=v);
        Select(appearance,"skin",T("原创皮肤","Original skin"),["ink","soft","terminal"],[T("墨色","Ink"),T("苔色","Moss"),T("终端","Terminal")],prefs.Skin,v=>prefs.Skin=v);
        Select(appearance,"language",T("语言（设置页重新打开后更新）","Language (reopen settings to translate)"),["zh","en"],["简体中文","English"],prefs.Language,v=>prefs.Language=v);
        Number(appearance,"font-size",T("标签字号","Label font size"),11,14,prefs.FontSize,v=>prefs.FontSize=v);
        Check(appearance,"comfortable",T("舒适密度（加宽主窗）","Comfortable density (wider window)"),prefs.Comfortable,v=>prefs.Comfortable=v);
        Number(appearance,"full-scale",T("完整窗缩放（%）","Full window scale (%)"),100,180,prefs.FullScalePercent,v=>prefs.FullScalePercent=v);
        var resetSize=new Button{Name="reset-size",AccessibleName="reset-size",Text=T("恢复 100% 大小","Reset size to 100%"),AutoSize=true,Margin=new Padding(0,8,0,8)};
        resetSize.Click+=(_,_)=>((NumericUpDown)appearance.Controls.Find("full-scale",true).Single()).Value=100;appearance.Controls.Add(resetSize);
        Note(appearance,T("完整窗可拖边框等比例缩放；不小于可读布局。无持续动画或网页内核。","Drag full-window borders to scale proportionally, with a readable minimum. No continuous animation or web engine."));
        var window=Page("window",T("窗口联动","Window"));
        Check(window,"topmost",T("始终置顶","Always on top"),prefs.Topmost,v=>prefs.Topmost=v);
        Check(window,"hover-expand",T("悬停展开小窗","Expand on hover"),prefs.HoverExpand,v=>prefs.HoverExpand=v);
        Check(window,"keep-expanded",T("悬停展开后保持展开","Keep expanded after hover"),prefs.KeepExpanded,v=>prefs.KeepExpanded=v);
        Check(window,"snap-edges",T("贴近屏幕边缘时吸附","Snap near screen edges"),prefs.Snap,v=>prefs.Snap=v);
        Check(window,"edge-collapse",T("完整窗拖至屏幕边缘后收为悬浮球","Collapse full window to orb when dragged to an edge"),prefs.EdgeCollapse,v=>prefs.EdgeCollapse=v);
        Check(window,"click-through",T("鼠标穿透（从托盘显示 / 解锁）","Click-through (unlock via tray)"),prefs.ClickThrough,v=>prefs.ClickThrough=v);
        Note(window,T("以 Codex 桌面窗口为准：最小化保留；所有窗口关闭后收起。连续两次确认消失才关闭，避免短暂切换造成误退。","Follows desktop windows: minimize keeps the widget; closing all windows dismisses it after two absent checks."));
        var follow=new CheckBox{Name="follow-at-login",AccessibleName="follow-at-login",Text=T("随 Codex 自动出现（登录时启动监听）","Follow Codex automatically (start watcher at sign-in)"),Checked=!demo&&FollowStartup.Enabled,Enabled=!demo,AutoSize=true,MaximumSize=new Size(388,0),Margin=new Padding(0,7,0,12)};
        bool settingFollow=false;
        follow.CheckedChanged+=(_,_)=>
        {
            if(settingFollow)return;
            try
            {
                if(follow.Checked&&MessageBox.Show(this,T("将添加当前用户的登录启动项。Codex 关闭后仅保留轻量监听，不刷新额度；可取消此选项移除启动项。是否启用？","Add a user sign-in startup entry? A lightweight watcher remains while Codex is closed, with no quota refresh. Uncheck to remove it."),"Quota Float",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)
                {settingFollow=true;follow.Checked=false;settingFollow=false;return;}
                FollowStartup.SetEnabled(follow.Checked);
            }
            catch(Exception e)when(e is UnauthorizedAccessException or System.Security.SecurityException or InvalidOperationException or IOException)
            {settingFollow=true;follow.Checked=FollowStartup.Enabled;settingFollow=false;MessageBox.Show(this,T("启动项未更改，请检查权限或旧版本的启动设置。","Startup entry was not changed. Check permissions or the old installation's setting."),"Quota Float");}
        };
        window.Controls.Add(follow);
        Note(window,T("开启后，照常点原 Codex 图标即可。无窗口时每 5 秒检查一次，有窗口时每 2 秒检查一次；托盘“停止联动并退出”可结束本次监听。不修改 Codex 快捷方式。","With following enabled, use the original Codex icon. Checks every 5s when idle and every 2s while visible. Quit from the tray to stop this session. Codex shortcuts are unchanged."));
        var data=Page("data",T("数据提醒","Data & alerts"));
        Check(data,"auto-refresh",T("自动刷新","Automatic refresh"),prefs.AutoRefresh,v=>prefs.AutoRefresh=v);
        Number(data,"refresh-seconds",T("正常刷新间隔（秒）","Normal refresh interval (seconds)"),60,1800,prefs.RefreshSeconds,v=>prefs.RefreshSeconds=v);
        Note(data,T("临近重置加快至约 60 秒；失败退避；手动刷新至少间隔 10 秒。","About 60s near reset; failures back off; manual refresh is limited to once per 10s."));
        var thresholds=new FlowLayoutPanel{Width=388,Height=76};
        var warning=new NumericUpDown{Name="warning-threshold",AccessibleName="warning-threshold",Minimum=2,Maximum=99,Value=prefs.Warning,Width=68};
        var critical=new NumericUpDown{Name="critical-threshold",AccessibleName="critical-threshold",Minimum=1,Maximum=98,Value=prefs.Critical,Width=68};
        thresholds.Controls.Add(new Label{Text=T("注意 %","Caution %"),AutoSize=true,Padding=new Padding(0,5,0,0)});thresholds.Controls.Add(warning);
        thresholds.Controls.Add(new Label{Text=T("紧急 %","Critical %"),AutoSize=true,Padding=new Padding(0,5,0,0)});thresholds.Controls.Add(critical);
        var apply=new Button{Text=T("应用阈值","Apply"),Name="apply-thresholds",AccessibleName="apply-thresholds",AutoSize=true};apply.Click+=(_,_)=>{if(critical.Value>=warning.Value){MessageBox.Show(this,T("紧急阈值必须低于注意阈值。","Critical must be lower than caution."),"Quota Float");return;}prefs.Warning=(int)warning.Value;prefs.Critical=(int)critical.Value;changed();};thresholds.Controls.Add(apply);data.Controls.Add(thresholds);
        Check(data,"notify",T("低额度提醒（Windows 通知，默认关闭）","Low-quota alerts (Windows notification, off by default)"),prefs.Notify,v=>prefs.Notify=v);
        Note(data,T("仅 GET 请求读取额度。凭证只用于 chatgpt.com，不落日志或配置。不兑换、购买、停止任务。","GET-only quota reads. Credentials go only to chatgpt.com; never to logs or settings. No redemption, purchases or task interruption."));
        var about=Page("about",T("关于","About"));
        Note(about,"Quota Float 0.3.0 · Windows x64"+(demo?" · DEMO":""));
        Note(about,T("这是首个原生版本，不是上游全功能交付。已实现真实额度读取及窗口/托盘联动；上游付费授权、自动更新与签名发布没有实现。","First native build, not full upstream parity. Live quota, native window/tray and lifecycle are implemented. Paid licensing, automatic updates and signed releases are not implemented."));
        Note(about,T("接口并非稳定公共 API，可能随 Codex 变化而失效。出现未知/未登录时请回 Codex 检查，不要把未知当作 0%。","The endpoint is not a stable public API. If unavailable, check Codex; unknown is not 0%."));
        Note(about,T("配置位置：","Preferences: ")+PreferenceStore.DirectoryPath+"\n"+T("只保存外观、行为与位置，不保存账户及额度。损坏时尝试 .bak 恢复。","Only appearance, behavior and position persist; never accounts or quota. Corrupt files fall back to .bak."));
        var resetButton=new Button{Text=T("恢复默认设置","Reset preferences"),Name="reset-preferences",AccessibleName="reset-preferences",AutoSize=true};resetButton.Click+=(_,_)=>{reset();Close();};about.Controls.Add(resetButton);
        ApplyTheme();
        Shown+=(_,_)=>{var area=Screen.FromControl(this).WorkingArea;Location=new Point(Math.Clamp(Left,area.Left,Math.Max(area.Left,area.Right-Width)),Math.Clamp(Top,area.Top,Math.Max(area.Top,area.Bottom-Height)));Activate();};
    }
    void UpdatePalette()
    {
        bool light=prefs.Theme=="light";
        if(prefs.Theme=="system")try{using var key=Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");light=key?.GetValue("AppsUseLightTheme") is int n&&n!=0;}catch{light=false;}
        panel=ColorTranslator.FromHtml(light?"#F3F6F4":prefs.Skin=="soft"?"#20322E":prefs.Skin=="terminal"?"#202522":"#1C2630");
        text=ColorTranslator.FromHtml(light?"#1C3037":"#EDF4F8");muted=ColorTranslator.FromHtml(light?"#54696D":"#ABBDC5");border=ColorTranslator.FromHtml(light?"#CBD6D7":"#435461");field=ColorTranslator.FromHtml(light?"#FFFFFF":"#273540");accent=ColorTranslator.FromHtml(light?"#237453":"#91CEB6");
    }
    void ApplyTheme()
    {
        UpdatePalette();
        void Style(Control c)
        {
            c.BackColor=panel;c.ForeColor=c.Tag as string=="muted"?muted:text;
            if(c is Button b){b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderSize=1;b.FlatAppearance.BorderColor=border;b.FlatAppearance.MouseOverBackColor=field;b.FlatAppearance.MouseDownBackColor=border;b.Cursor=Cursors.Hand;}
            if(c.Name=="mode-"+prefs.Mode+"-button"){c.BackColor=field;c.ForeColor=accent;}
            if(c is ComboBox combo){combo.FlatStyle=FlatStyle.Flat;c.BackColor=field;}
            if(c is NumericUpDown numeric){numeric.BorderStyle=BorderStyle.FixedSingle;c.BackColor=field;}
            if(c.Tag as string=="primary"){c.BackColor=accent;c.ForeColor=panel;}
            foreach(Control child in c.Controls)Style(child);
        }
        Style(this);
        foreach(var (button,page) in pages){bool selected=button.Name=="tab-"+selectedPage;button.BackColor=selected?field:panel;button.ForeColor=selected?accent:muted;button.FlatAppearance.BorderSize=selected?1:0;}
        Invalidate(true);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);using var pen=new Pen(border);e.Graphics.DrawRectangle(pen,0,0,ClientSize.Width-1,ClientSize.Height-1);
        float scale=DeviceDpi/96f;e.Graphics.DrawLine(pen,1,76*scale,Width-2,76*scale);e.Graphics.DrawLine(pen,1,Height-60*scale,Width-2,Height-60*scale);
    }
    void Note(FlowLayoutPanel page,string text)=>page.Controls.Add(new Label{Text=text,AutoSize=true,MaximumSize=new Size(388,0),Margin=new Padding(0,5,0,15),Tag="muted",ForeColor=muted});
    void Check(FlowLayoutPanel page,string id,string label,bool value,Action<bool> update)
    {
        var box=new CheckBox{Text=label,Name=id,AccessibleName=id,Checked=value,AutoSize=true,MaximumSize=new Size(388,0),Margin=new Padding(0,7,0,12)};
        box.CheckedChanged+=(_,_)=>{update(box.Checked);changed();};page.Controls.Add(box);
    }
    void Select(FlowLayoutPanel page,string id,string label,string[] values,string[] labels,string current,Action<string> update)
    {
        page.Controls.Add(new Label{Text=label,AutoSize=true,Margin=new Padding(0,8,0,3)});
        var box=new ComboBox{Name=id,AccessibleName=id,DropDownStyle=ComboBoxStyle.DropDownList,Width=320};box.Items.AddRange(labels);box.SelectedIndex=Math.Max(0,Array.IndexOf(values,current));box.SelectedIndexChanged+=(_,_)=>{update(values[box.SelectedIndex]);changed();};page.Controls.Add(box);
    }
    void Number(FlowLayoutPanel page,string id,string label,int min,int max,int value,Action<int> update)
    {
        page.Controls.Add(new Label{Text=label,AutoSize=true,Margin=new Padding(0,8,0,3)});
        var number=new NumericUpDown{Name=id,AccessibleName=id,Minimum=min,Maximum=max,Value=Math.Clamp(value,min,max),Width=110};number.ValueChanged+=(_,_)=>{update((int)number.Value);changed();};page.Controls.Add(number);
    }
}
