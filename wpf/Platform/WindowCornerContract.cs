namespace QuotaFloat.Wpf.Platform;

internal sealed class WindowCornerContract
{
    internal const string CivicIdentity = "QF-CORNERS-DWM-ROUNDSMALL-B3-OPAQUE";

    private WindowCornerContract(int nativeCornerPreference, uint borderColor)
    {
        NativeCornerPreference = nativeCornerPreference;
        BorderColor = borderColor;
    }

    internal static WindowCornerContract Civic { get; } = new(3, 0x003DADE2);

    internal int NativeCornerPreference { get; }
    internal uint BorderColor { get; }
}
