using System.Windows.Forms;

internal static class HostProgram
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var form = new Form
        {
            Text = "QuotaFloat lifecycle controlled host",
            Width = 320,
            Height = 160,
            ShowInTaskbar = true
        };
        form.Show();
        Application.Run(form);
    }
}
