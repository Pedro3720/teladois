namespace TelaDois.Core.Video;

public static class FfmpegLocator
{
    /// <summary>
    /// Procura <c>ffmpeg.exe</c> primeiro em <c>ffmpeg\</c> ao lado do TelaDois.exe (o instalador põe ali) e depois nas
    /// pastas do PATH (o winget põe um atalho em WinGet\Links).
    /// </summary>
    public static string? Find(string? path = null, string? appDirectory = null)
    {
        var bundled = Path.Combine(appDirectory ?? AppContext.BaseDirectory, "ffmpeg", "ffmpeg.exe");
        if (File.Exists(bundled))
        {
            return bundled;
        }
        path ??= Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var candidate = Path.Combine(dir, "ffmpeg.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }
        return null;
    }
}
