using System.Diagnostics;

namespace TelaDois.Tray;

/// <summary>Registro em texto para diagnosticar a conexão sem depurador.</summary>
internal sealed class FileLog(string path)
{
    private readonly Lock _gate = new();

    public static FileLog Default()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TelaDois");
        Directory.CreateDirectory(folder);
        return new FileLog(Path.Combine(folder, "tela-dois.log"));
    }

    public void Write(string message)
    {
        lock (_gate)
        {
            File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
    }

    public void Open() => Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
}
