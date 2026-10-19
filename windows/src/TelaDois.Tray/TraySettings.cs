using System.Text.Json;

namespace TelaDois.Tray;

/// <summary>Preferências guardadas em %LOCALAPPDATA%\TelaDois\settings.json.</summary>
internal sealed class TraySettings
{
    public string? Monitor { get; set; }

    private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TelaDois", "settings.json");

    public static TraySettings Load()
    {
        try
        {
            return JsonSerializer.Deserialize<TraySettings>(File.ReadAllText(FilePath)) ?? new TraySettings();
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            return new TraySettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
    }
}
