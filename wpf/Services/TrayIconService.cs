using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace QuotaFloat.Wpf.Services;

public sealed class TrayIconService : IDisposable
{
    public const int OpenSettingsCommandId = 0x5101;
    public const int ExitCommandId = 0x5102;

    private const int WmApp = 0x8000;
    private const int WmTrayCallback = WmApp + 0x4F;
    private const int WmRButtonUp = 0x0205;
    private const int WmLButtonDblClick = 0x0203;
    private const uint NimAdd = 0x00000000;
    private const uint NimDelete = 0x00000002;
    private const uint NifMessage = 0x00000001;
    private const uint NifIcon = 0x00000002;
    private const uint NifTip = 0x00000004;
    private const uint MfString = 0x00000000;
    private const uint MfEnabled = 0x00000000;
    private const uint TpmLeftAlign = 0x0000;
    private const uint TpmBottomAlign = 0x0020;
    private const uint TpmReturnCommand = 0x0100;
    private const uint TpmRightButton = 0x0002;

    private readonly Action openSettings;
    private readonly Action exitApplication;
    private readonly HwndSource hwndSource;
    private readonly uint iconId = 0x5146;
    private bool disposed;
    private bool iconAdded;

    public TrayIconService(Window owner, Action openSettings, Action exitApplication)
    {
        this.openSettings = openSettings;
        this.exitApplication = exitApplication;

        var hwnd = new WindowInteropHelper(owner).Handle;
        hwndSource = HwndSource.FromHwnd(hwnd)
            ?? throw new InvalidOperationException("Unable to attach the WPF tray message hook.");
        hwndSource.AddHook(WindowMessageHook);
        AddIcon(hwnd);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (iconAdded)
        {
            var data = CreateIconData(hwndSource.Handle, NifMessage | NifIcon | NifTip);
            Shell_NotifyIcon(NimDelete, ref data);
            iconAdded = false;
        }

        hwndSource.RemoveHook(WindowMessageHook);
        GC.SuppressFinalize(this);
    }

    private void AddIcon(IntPtr hwnd)
    {
        var data = CreateIconData(hwnd, NifMessage | NifIcon | NifTip);
        if (!Shell_NotifyIcon(NimAdd, ref data))
        {
            throw new InvalidOperationException("Unable to create the Windows notification-area icon.");
        }

        iconAdded = true;
    }

    private NOTIFYICONDATA CreateIconData(IntPtr hwnd, uint flags) => new()
    {
        cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
        hWnd = hwnd,
        uID = iconId,
        uFlags = flags,
        uCallbackMessage = WmTrayCallback,
        hIcon = LoadIcon(IntPtr.Zero, new IntPtr(32512)),
        szTip = "Quota Float"
    };

    private IntPtr WindowMessageHook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message != WmTrayCallback || unchecked((uint)wParam.ToInt64()) != iconId)
        {
            return IntPtr.Zero;
        }

        var notification = unchecked((uint)lParam.ToInt64());
        if (notification == WmLButtonDblClick)
        {
            openSettings();
            handled = true;
        }
        else if (notification == WmRButtonUp)
        {
            ShowContextMenu(hwnd);
            handled = true;
        }

        return IntPtr.Zero;
    }

    private void ShowContextMenu(IntPtr hwnd)
    {
        var menu = CreatePopupMenu();
        if (menu == IntPtr.Zero)
        {
            return;
        }

        try
        {
            AppendMenu(menu, MfString | MfEnabled, OpenSettingsCommandId, "打开设置");
            AppendMenu(menu, MfString | MfEnabled, ExitCommandId, "退出 Quote Float");
            SetForegroundWindow(hwnd);
            if (!GetCursorPos(out var point))
            {
                return;
            }

            var command = TrackPopupMenu(
                menu,
                TpmLeftAlign | TpmBottomAlign | TpmReturnCommand | TpmRightButton,
                point.X,
                point.Y,
                0,
                hwnd,
                IntPtr.Zero);
            switch (command)
            {
                case OpenSettingsCommandId:
                    openSettings();
                    break;
                case ExitCommandId:
                    exitApplication();
                    break;
            }

            PostMessage(hwnd, 0, IntPtr.Zero, IntPtr.Zero);
        }
        finally
        {
            DestroyMenu(menu);
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Shell_NotifyIcon(uint message, ref NOTIFYICONDATA data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadIcon(IntPtr instance, IntPtr iconName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AppendMenu(IntPtr menu, uint flags, int commandId, string text);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyMenu(IntPtr menu);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int TrackPopupMenu(IntPtr menu, uint flags, int x, int y, int reserved, IntPtr owner, IntPtr rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }
}
