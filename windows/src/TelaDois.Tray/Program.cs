namespace TelaDois.Tray;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var single = new Mutex(true, @"Local\TelaDois.Tray", out var isFirst);
        if (!isFirst)
        {
            return;
        }
        ApplicationConfiguration.Initialize();
        Application.Run(new TrayContext());
    }
}
