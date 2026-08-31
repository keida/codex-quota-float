using System.Diagnostics;
using System.Runtime.InteropServices;
using QuotaFloat;

internal static class FollowRegression
{
    [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr handle,int command);
    public static void Run(string hostPath)
    {
        Exception? failure=null;
        var thread=new Thread(()=>
        {
            var hosts=new List<Process>();
            try
            {
                Process StartHost()
                {
                    var host=Process.Start(new ProcessStartInfo(hostPath){UseShellExecute=false,CreateNoWindow=true})!;hosts.Add(host);
                    var until=DateTime.UtcNow.AddSeconds(4);
                    while(host.MainWindowHandle==IntPtr.Zero&&DateTime.UtcNow<until){Thread.Sleep(25);host.Refresh();}
                    if(host.MainWindowHandle==IntPtr.Zero)throw new Exception("Controlled host window unavailable");return host;
                }
                using var lifecycle=new CodexLifecycle();var host=StartHost();
                if(!lifecycle.AttachTestProcessAsync(host.Id,CancellationToken.None).GetAwaiter().GetResult())throw new Exception("Controlled attach failed");
                using var follow=new FollowContext(lifecycle,true,true,null);
                using var step=new System.Windows.Forms.Timer{Interval=100};
                int phase=0;var deadline=DateTime.UtcNow.AddSeconds(10);var hold=DateTime.MinValue;
                step.Tick+=(_,_)=>
                {
                    try
                    {
                        if(DateTime.UtcNow>deadline)throw new Exception($"Follow regression timed out in phase {phase}");
                        switch(phase)
                        {
                            case 0 when follow.WidgetVisible:
                                Console.WriteLine("PASS: watcher opens widget for controlled desktop window");ShowWindow(host.MainWindowHandle,6);hold=DateTime.UtcNow.AddSeconds(3);phase=1;break;
                            case 1 when DateTime.UtcNow>=hold:
                                if(!follow.WidgetVisible)throw new Exception("Minimization closed widget");
                                Console.WriteLine("PASS: minimize retains widget");ShowWindow(host.MainWindowHandle,0);phase=2;deadline=DateTime.UtcNow.AddSeconds(8);break;
                            case 2 when !follow.WidgetVisible:
                                if(host.HasExited)throw new Exception("Host should still run with hidden window");
                                Console.WriteLine("PASS: hidden desktop window closes widget while host process remains alive");
                                ShowWindow(host.MainWindowHandle,9);phase=3;deadline=DateTime.UtcNow.AddSeconds(8);break;
                            case 3 when follow.WidgetVisible:
                                Console.WriteLine("PASS: existing desktop window reopening restores widget");host.CloseMainWindow();phase=4;deadline=DateTime.UtcNow.AddSeconds(8);break;
                            case 4 when !follow.WidgetVisible:
                                host=StartHost();phase=5;deadline=DateTime.UtcNow.AddSeconds(8);break;
                            case 5 when follow.WidgetVisible:
                                Console.WriteLine("PASS: fresh desktop process reopening restores widget");
                                phase=6;Application.OpenForms.OfType<QuotaForm>().Single().Close();break;
                        }
                    }
                    catch(Exception error){failure=error;follow.ExitThread();}
                };
                step.Start();Application.Run(follow);
                if(phase!=6&&failure is null)throw new Exception("Coordinator exited before all transitions completed");
                Console.WriteLine("PASS: explicit widget quit ends watcher session");
            }
            catch(Exception error){failure=error;}
            finally
            {
                foreach(var host in hosts)
                {
                    if(!host.HasExited){host.CloseMainWindow();if(!host.WaitForExit(2000)){host.Kill();host.WaitForExit(2000);}}
                    host.Dispose();
                }
            }
        });
        thread.SetApartmentState(ApartmentState.STA);thread.Start();
        if(!thread.Join(45000))throw new Exception("Follow regression did not terminate within 45s");
        if(failure is not null)throw failure;
    }
}
