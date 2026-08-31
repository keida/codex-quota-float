using System.Drawing;
using System.Runtime.InteropServices;
using QuotaFloat;

internal static class ResizeSnapRegression
{
    public static void Run()
    {
        Check(NativeWindowTools.GetResizeHitTest(new Rectangle(100, 100, 306, 286), new Point(100, 100), 8, true) == NativeWindowTools.ResizeHitTest.TopLeft, "Full window exposes top-left resize hit-test");
        Check(NativeWindowTools.GetResizeHitTest(new Rectangle(100, 100, 306, 286), new Point(405, 240), 8, true) == NativeWindowTools.ResizeHitTest.Right, "Full window exposes right resize hit-test");
        Check(NativeWindowTools.GetResizeHitTest(new Rectangle(100, 100, 306, 286), new Point(405, 240), 8, false) == NativeWindowTools.ResizeHitTest.Client, "Compact modes do not expose resize hit-tests");

        var limits=QuotaForm.GetFullSizeLimits(new Rectangle(0, 0, 1920, 1040), 1f, 100);
        Check(limits.Minimum==new Size(306, 286), "Full minimum is 306x286 logical pixels");
        Check(QuotaForm.GetFullSizeLimits(new Rectangle(0,0,1920,1040),1f,150).Minimum==limits.Minimum,"Enlarging does not raise the minimum resize limit");
        Check(limits.Maximum.Width<=Math.Min(1920-8, (int)Math.Round(306*1.8))&&limits.Maximum.Height<=Math.Min(1040-8, (int)Math.Round(286*1.8)), "Full maximum is bounded by scale and working area");
        var proportional=NativeWindowTools.ConstrainProportionalSize(new Size(450,286),limits,306d/286d);
        Check(Math.Abs(proportional.Width/(double)proportional.Height-306d/286d)<.01&&proportional.Width<=limits.Maximum.Width&&proportional.Height<=limits.Maximum.Height, "Full WM_SIZING seam preserves whole-UI aspect ratio within bounds");
        Check(NativeWindowTools.ShouldConvertToOrb(new Rectangle(0, 100, 306, 286), new Rectangle(0, 0, 1920, 1040), 0), "Exact edge release converts full widget to orb");
        Check(!NativeWindowTools.ShouldConvertToOrb(new Rectangle(4, 100, 306, 286), new Rectangle(0, 0, 1920, 1040), 0), "Near-edge release does not convert before snap");

        Exception? failure=null;
        var thread=new Thread(() =>
        {
            try
            {
            using var lifecycle=new CodexLifecycle();
            var preferences=new Preferences{Topmost=false,HoverExpand=false};
            using var owner=new QuotaForm(preferences,new PreferenceStore(false),lifecycle,true,null);
            owner.Show();
            Application.DoEvents();
            try
            {
                owner.Bounds=new Rectangle(100,100,500,400);
                Check(NcHitTest(owner.Handle,new Point(owner.Left,owner.Top))==13, "Actual WM_NCHITTEST returns top-left resize code for full window");
                Check(NcHitTest(owner.Handle,new Point(owner.Right-1,owner.Top+200))==11, "Actual WM_NCHITTEST returns right resize code for full window");
                var orb=(Button)owner.Controls.Find("orb-button", true).Single();
                Check(orb.AccessibleDescription?.Contains("小球", StringComparison.Ordinal)==true, "Full window directly exposes a discoverable orb button");
                Check(orb.AccessibleName=="orb-button", "Orb button has stable accessibility id");
                owner.ClientSize=new Size(306,286);
                ResizeBottom(owner,400);
                Check(preferences.FullScalePercent==140&&Math.Abs(owner.Width/(double)owner.Height-306d/286d)<.01,"Actual bottom-border sizing scales both axes and persists 140%");
                ((Button)owner.Controls.Find("orb-button",true).Single()).PerformClick();
                Check(owner.Width==80,"Orb button directly switches to compact orb");
                ((Button)owner.Controls.Find("expand-button",true).Single()).PerformClick();
                Check(owner.Width==(int)Math.Round(306*1.4),"Orb expansion restores the saved full-window scale");
                ResizeBottom(owner,10);
                Check(preferences.FullScalePercent==100&&owner.ClientSize==new Size(306,286),"Actual border resize stops at readable minimum after enlargement");
                var area=Screen.FromControl(owner).WorkingArea;
                owner.Location=new Point(area.Right-owner.Width,area.Top+60);
                EndMove(owner);
                Check(preferences.Mode=="orb"&&owner.Right==area.Right-4,"Drag completion at right edge docks the whole orb to that edge");
                ((Button)owner.Controls.Find("expand-button",true).Single()).PerformClick();
                preferences.EdgeCollapse=false;owner.Location=new Point(area.Right-owner.Width,area.Top+60);EndMove(owner);
                Check(preferences.Mode=="full","Disabling edge-collapse keeps full mode at the edge");
                ((Button)owner.Controls.Find("collapse-button", true).Single()).PerformClick();
                Application.DoEvents();
                Check(NcHitTest(owner.Handle,new Point(owner.Left,owner.Top))==1, "Actual WM_NCHITTEST keeps compact mode non-resizable");
            }
            finally { owner.Close(); }
            }
            catch(Exception error){failure=error;}
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Check(thread.Join(8000), "Full window accessibility probe completes");
        if(failure is not null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    [DllImport("user32.dll")]
    static extern IntPtr SendMessage(IntPtr handle,int message,IntPtr wParam,IntPtr lParam);
    static int NcHitTest(IntPtr handle,Point point)=>unchecked((int)SendMessage(handle,0x84,IntPtr.Zero,new IntPtr((point.Y<<16)|(point.X&0xffff))).ToInt64());

    [StructLayout(LayoutKind.Sequential)]struct NativeRect{public int Left,Top,Right,Bottom;}
    static void ResizeBottom(QuotaForm owner,int height)
    {
        NcHitTest(owner.Handle,new Point(owner.Left+owner.Width/2,owner.Bottom-2));
        SendMessage(owner.Handle,0x231,IntPtr.Zero,IntPtr.Zero);
        var ptr=Marshal.AllocHGlobal(Marshal.SizeOf<NativeRect>());
        try
        {
            Marshal.StructureToPtr(new NativeRect{Left=owner.Left,Top=owner.Top,Right=owner.Right,Bottom=owner.Top+height},ptr,false);
            SendMessage(owner.Handle,0x214,new IntPtr(6),ptr);var rect=Marshal.PtrToStructure<NativeRect>(ptr);
            owner.Bounds=Rectangle.FromLTRB(rect.Left,rect.Top,rect.Right,rect.Bottom);
        }
        finally{Marshal.FreeHGlobal(ptr);}
        SendMessage(owner.Handle,0x232,IntPtr.Zero,IntPtr.Zero);
    }
    static void EndMove(QuotaForm owner)
    {
        NcHitTest(owner.Handle,new Point(owner.Left+owner.Width/2,owner.Top+60));
        SendMessage(owner.Handle,0x231,IntPtr.Zero,IntPtr.Zero);SendMessage(owner.Handle,0x232,IntPtr.Zero,IntPtr.Zero);
    }

    static void Check(bool condition,string name)
    {
        if(!condition)throw new Exception("FAIL: "+name);
        Console.WriteLine("PASS: "+name);
    }
}
