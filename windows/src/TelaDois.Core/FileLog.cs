using System.Diagnostics;

namespace TelaDois.Core;

/// <summary>Registro em texto para diagnosticar a conexão sem depurador.</summary>
public sealed class FileLog(string path)
{
    private readonly Lock _gate = new();

    public static FileLog Default()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TelaDois");
        try
        {
            Directory.CreateDirectory(folder);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Sem pasta, as linhas são descartadas em Write; o app abre do mesmo jeito.
        }
        return new FileLog(Path.Combine(folder, "tela-dois.log"));
    }

    /// <summary>
    /// Nunca lança: é chamado de laços e manipuladores que não podem cair por causa do registro (envio de vídeo, atalho,
    /// interface). Se o arquivo estiver preso (antivírus, outra instância) ou a pasta sumir, a linha é descartada.
    /// </summary>
    public void Write(string message)
    {
        lock (_gate)
        {
            try
            {
                File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}");
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    public void Open() => Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
}
