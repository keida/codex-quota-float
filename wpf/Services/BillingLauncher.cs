using System.Diagnostics;
using System.ComponentModel;

namespace QuotaFloat.Wpf.Services;

public sealed class BillingLauncher
{
    public const string UsageBillingUri = "https://chatgpt.com/codex/settings/usage";

    private readonly Func<string, bool> launch;

    public BillingLauncher(Func<string, bool>? launch = null)
    {
        this.launch = launch ?? (uri =>
        {
            try
            {
                return Process.Start(new ProcessStartInfo { FileName = uri, UseShellExecute = true }) is not null;
            }
            catch (Win32Exception) { return false; }
            catch (InvalidOperationException) { return false; }
        });
    }

    public bool TryOpenUsageBilling()
    {
        if (!Uri.TryCreate(UsageBillingUri, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !string.Equals(uri.Host, "chatgpt.com", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(uri.AbsolutePath, "/codex/settings/usage", StringComparison.Ordinal))
        {
            return false;
        }

        return launch(uri.AbsoluteUri);
    }
}

