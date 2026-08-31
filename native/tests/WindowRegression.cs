using QuotaFloat;
using System.Runtime.InteropServices;

internal static class WindowRegression
{
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")]static extern IntPtr GetWindowLongPtr(IntPtr handle,int index);
    [DllImport("user32.dll")]static extern bool GetLayeredWindowAttributes(IntPtr handle,out uint color,out byte alpha,out uint flags);
    public static void Run()
    {
        Exception? failure=null;
        var thread=new Thread(()=>
        {
            try
            {
                using var lifecycle=new CodexLifecycle();
                using var owner=new QuotaForm(new Preferences{Topmost=true,HoverExpand=false},new PreferenceStore(false),lifecycle,true,null);
                using var timeout=new System.Windows.Forms.Timer{Interval=4000};
                using var probe=new System.Windows.Forms.Timer{Interval=100};
                bool checkedDialog=false;
                timeout.Tick+=(_,_)=>{failure??=new Exception("Settings dialog did not appear within 4s");foreach(Form f in owner.OwnedForms)f.Close();owner.Close();};
                probe.Tick+=(_,_)=>
                {
                    var dialog=owner.OwnedForms.OfType<SettingsForm>().FirstOrDefault();
                    if(dialog is null)return;
                    probe.Stop();
                    try
                    {
                        if(!dialog.Visible||!dialog.TopMost||owner.TopMost)throw new Exception("Settings must own topmost layer; widget must yield it while modal is open");
                        var close=dialog.Controls.Find("settings-close",true).Single();
                        if(!dialog.RectangleToScreen(dialog.ClientRectangle).Contains(close.RectangleToScreen(close.ClientRectangle)))throw new Exception("Settings close button is outside the window");
                        foreach(var id in new[]{"mode-full-button","mode-mini-button","mode-orb-button"})
                            if(dialog.Controls.Find(id,true).Length!=1)throw new Exception("Window modes need visible, direct buttons; missing "+id);
                        var originalSize=owner.ClientSize;
                        var scale=(NumericUpDown)dialog.Controls.Find("full-scale",true).Single();
                        scale.Value=150;
                        if(owner.ClientSize.Width<=originalSize.Width)throw new Exception("Settings scale did not enlarge full widget");
                        ((Button)dialog.Controls.Find("reset-size",true).Single()).PerformClick();
                        if(owner.ClientSize!=originalSize)throw new Exception("Reset to 100% did not restore original dimensions");
                        ((Button)dialog.Controls.Find("mode-mini-button",true).Single()).PerformClick(); // Real settings callback rebuilds the owner.
                        if(owner.TopMost||!dialog.TopMost)throw new Exception("Changing settings raised widget above modal");
                        var native=typeof(QuotaForm).Assembly.GetType("QuotaFloat.NativeWindowTools")!;
                        var clickThrough=native.GetMethod("ClickThrough")!;
                        clickThrough.Invoke(null,[owner,true]);
                        if(!GetLayeredWindowAttributes(owner.Handle,out _,out var alpha,out var flags)||alpha!=255||(flags&2)==0)throw new Exception("Click-through creates a layered window without visible alpha");
                        clickThrough.Invoke(null,[owner,false]);
                        if((GetWindowLongPtr(owner.Handle,-20).ToInt64()&0x80020)!=0)throw new Exception("Unlock leaves stale layered/transparent window styles");
                        checkedDialog=true;
                    }
                    catch(Exception error){failure=error;}
                    finally{dialog.Close();owner.BeginInvoke((Action)(()=>owner.Close()));}
                };
                owner.Shown+=(_,_)=>owner.BeginInvoke((Action)(()=>((Button)owner.Controls.Find("settings-button",true).Single()).PerformClick()));
                probe.Start();timeout.Start();Application.Run(owner);
                if(!checkedDialog&&failure is null)failure=new Exception("Dialog assertions were not reached");
            }
            catch(Exception error){failure=error;}
        });
        thread.SetApartmentState(ApartmentState.STA);thread.Start();
        if(!thread.Join(8000))throw new Exception("Window regression timed out");
        if(failure is not null)throw failure;
    }
}
