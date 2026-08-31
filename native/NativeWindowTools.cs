using System.Runtime.InteropServices;

namespace QuotaFloat;

public static class NativeWindowTools
{
    public enum ResizeHitTest
    {
        None=0,Client=1,Left=10,Right=11,Top=12,TopLeft=13,TopRight=14,Bottom=15,BottomLeft=16,BottomRight=17
    }
    public readonly record struct SizeLimits(Size Minimum,Size Maximum);

    public static readonly int RestoreMessage=RegisterWindowMessage("QuotaFloat.ShowExisting.v1");
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int RegisterWindowMessage(string name);
    [DllImport("user32.dll")]static extern bool PostMessage(IntPtr handle,int message,IntPtr wParam,IntPtr lParam);
    [DllImport("user32.dll")]static extern bool ReleaseCapture();
    [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr handle,int message,IntPtr wParam,IntPtr lParam);
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")]static extern IntPtr GetWindowLongPtr(IntPtr handle,int index);
    [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")]static extern IntPtr SetWindowLongPtr(IntPtr handle,int index,IntPtr value);
    [DllImport("user32.dll")]static extern bool SetLayeredWindowAttributes(IntPtr handle,uint colorKey,byte alpha,uint flags);
    [DllImport("user32.dll")]static extern bool SetWindowPos(IntPtr handle,IntPtr insertAfter,int x,int y,int cx,int cy,uint flags);
    public static void ShowExisting()=>PostMessage(new IntPtr(0xffff),RestoreMessage,IntPtr.Zero,IntPtr.Zero);
    public static void Drag(Form form){ReleaseCapture();SendMessage(form.Handle,0xA1,new IntPtr(2),IntPtr.Zero);}
    public static ResizeHitTest GetResizeHitTest(Rectangle windowBounds,Point screenPoint,int border,bool resizable)
    {
        if(!windowBounds.Contains(screenPoint))return ResizeHitTest.None;
        if(!resizable)return ResizeHitTest.Client;
        border=Math.Max(1,border);
        bool left=screenPoint.X<windowBounds.Left+border;
        bool right=screenPoint.X>=windowBounds.Right-border;
        bool top=screenPoint.Y<windowBounds.Top+border;
        bool bottom=screenPoint.Y>=windowBounds.Bottom-border;
        if(top&&left)return ResizeHitTest.TopLeft;
        if(top&&right)return ResizeHitTest.TopRight;
        if(bottom&&left)return ResizeHitTest.BottomLeft;
        if(bottom&&right)return ResizeHitTest.BottomRight;
        if(left)return ResizeHitTest.Left;
        if(right)return ResizeHitTest.Right;
        if(top)return ResizeHitTest.Top;
        if(bottom)return ResizeHitTest.Bottom;
        return ResizeHitTest.Client;
    }
    public static bool ShouldConvertToOrb(Rectangle bounds,Rectangle workingArea,int edgeTolerance=0)
    {
        edgeTolerance=Math.Max(0,edgeTolerance);
        return bounds.Left<=workingArea.Left+edgeTolerance||bounds.Top<=workingArea.Top+edgeTolerance||
               bounds.Right>=workingArea.Right-edgeTolerance||bounds.Bottom>=workingArea.Bottom-edgeTolerance;
    }
    public static Size ConstrainProportionalSize(Size requested,SizeLimits limits,double aspectRatio)
    {
        if(aspectRatio<=0||double.IsNaN(aspectRatio)||double.IsInfinity(aspectRatio))return new Size(Math.Clamp(requested.Width,limits.Minimum.Width,limits.Maximum.Width),Math.Clamp(requested.Height,limits.Minimum.Height,limits.Maximum.Height));
        int width=Math.Clamp(requested.Width,limits.Minimum.Width,limits.Maximum.Width);
        int height=(int)Math.Round(width/aspectRatio);
        if(height>limits.Maximum.Height){height=limits.Maximum.Height;width=(int)Math.Round(height*aspectRatio);}
        if(height<limits.Minimum.Height){height=limits.Minimum.Height;width=(int)Math.Round(height*aspectRatio);}
        width=Math.Clamp(width,limits.Minimum.Width,limits.Maximum.Width);height=Math.Clamp((int)Math.Round(width/aspectRatio),limits.Minimum.Height,limits.Maximum.Height);
        return new Size(width,height);
    }
    public static void ClickThrough(Form form,bool enabled)
    {
        long style=GetWindowLongPtr(form.Handle,-20).ToInt64();
        const long transparent=0x20,layered=0x80000;
        long next=enabled?style|transparent|layered:style&~(transparent|layered);
        if(next==style)return;
        SetWindowLongPtr(form.Handle,-20,new IntPtr(next));
        if(enabled)SetLayeredWindowAttributes(form.Handle,0,255,2); // LWA_ALPHA: fully visible, input passes through.
        SetWindowPos(form.Handle,IntPtr.Zero,0,0,0,0,0x37); // no move/size/z-order/activation; refresh frame styles
        form.Invalidate();
    }
}
