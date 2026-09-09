using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace QuotaFloat.Wpf.Platform;

internal static class DwmWindowAppearance
{
    private const int DwmWindowCornerPreference = 33;
    private const int DwmBorderColor = 34;

    internal static string ContractIdentity => WindowCornerContract.CivicIdentity;

    internal static void Apply(Window window, WindowCornerContract contract)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        var cornerPreference = contract.NativeCornerPreference;
        _ = DwmSetWindowAttribute(hwnd, DwmWindowCornerPreference, ref cornerPreference, sizeof(int));

        var borderColor = contract.BorderColor;
        _ = DwmSetWindowAttribute(hwnd, DwmBorderColor, ref borderColor, sizeof(uint));
    }

    [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute", SetLastError = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);

    [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute", SetLastError = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref uint value, int valueSize);
}
