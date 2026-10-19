using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using TelaDois.Core.Link;

namespace TelaDois.Core.Video;

/// <summary>Roda o FFmpeg e entrega o stdout separado em access units.</summary>
public sealed partial class FfmpegCapture : IVideoStream
{
    /// <summary>AUD + fatia vazia: empurra para fora o último quadro quando o FFmpeg termina.</summary>
    private static readonly byte[] FlushMarker = [0, 0, 0, 1, 0x09, 0xF0, 0, 0, 1, 0x41, 0x9A, 0x02];

    private readonly Process _process;
    private readonly Action<string> _log;

    private FfmpegCapture(Process process, VideoConfig config, Action<string> log)
    {
        _process = process;
        Config = config;
        _log = log;
    }

    public VideoConfig Config { get; }

    public static FfmpegCapture Start(string ffmpegPath, IReadOnlyList<string> args, VideoConfig config, Action<string> log)
    {
        var process = Process.Start(StartInfo(ffmpegPath, args, redirectOutput: true)) ?? throw new IOException("não foi possível iniciar o FFmpeg");
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                log($"ffmpeg: {e.Data}");
            }
        };
        process.BeginErrorReadLine();
        return new FfmpegCapture(process, config, log);
    }

    public async IAsyncEnumerable<byte[]> AccessUnitsAsync([EnumeratorCancellation] CancellationToken ct)
    {
        var splitter = new H264AccessUnitSplitter();
        var buffer = new byte[256 * 1024];
        var stdout = _process.StandardOutput.BaseStream;
        while (true)
        {
            var read = await stdout.ReadAsync(buffer, ct);
            if (read == 0)
            {
                break;
            }
            foreach (var unit in splitter.Append(buffer.AsSpan(0, read)))
            {
                yield return unit;
            }
        }
        foreach (var unit in splitter.Append(FlushMarker))
        {
            yield return unit;
        }
    }

    /// <summary>GOP de 1 s: o próximo keyframe chega em até 1 s (o FFmpeg não aceita pedido no meio).</summary>
    public void RequestKeyframe() => _log("iPhone pediu keyframe (o próximo chega em até 1 s)");

    public async ValueTask DisposeAsync()
    {
        if (!_process.HasExited)
        {
            try
            {
                // "q" no stdin encerra o FFmpeg com calma; se não sair, mata.
                await _process.StandardInput.WriteAsync('q');
                await _process.StandardInput.FlushAsync();
            }
            catch (IOException)
            {
            }
            using var wait = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            try
            {
                await _process.WaitForExitAsync(wait.Token);
            }
            catch (OperationCanceledException)
            {
                _process.Kill(entireProcessTree: true);
            }
        }
        _process.Dispose();
    }

    /// <summary><c>h264_amf</c> se a GPU AMD codifica um quadro de teste; senão <c>libx264</c> (spec §5).</summary>
    public static async Task<string> ChooseEncoderAsync(string ffmpegPath, CancellationToken ct)
    {
        string[] args = ["-hide_banner", "-loglevel", "error", "-f", "lavfi", "-i", "testsrc2=size=640x360:rate=30",
            "-frames:v", "5", "-pix_fmt", "nv12", "-c:v", "h264_amf", "-f", "null", "-"];
        var (exitCode, _) = await RunAsync(ffmpegPath, args, ct);
        return exitCode == 0 ? "h264_amf" : "libx264";
    }

    /// <summary>Índice do <c>ddagrab</c> cuja tela tem <paramref name="width"/>×<paramref name="height"/>.</summary>
    public static async Task<int?> FindOutputIndexAsync(string ffmpegPath, uint width, uint height, CancellationToken ct)
    {
        for (var index = 0; index < 6; index++)
        {
            var (exitCode, log) = await RunAsync(ffmpegPath, FfmpegArgs.ProbeOutput(index), ct);
            if (exitCode != 0)
            {
                return null; // acabaram as saídas
            }
            var match = SizeRegex().Match(log);
            if (match.Success
                && uint.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) == width
                && uint.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) == height)
            {
                return index;
            }
        }
        return null;
    }

    /// <summary>FFmpeg no PATH ou instalado pelo winget (Gyan.FFmpeg).</summary>
    public static string? Locate()
    {
        foreach (var folder in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            if (folder.Length == 0)
            {
                continue;
            }
            var candidate = Path.Combine(folder.Trim(), "ffmpeg.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }
        var winget = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WinGet", "Packages");
        return Directory.Exists(winget)
            ? Directory.EnumerateFiles(winget, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault()
            : null;
    }

    private static async Task<(int ExitCode, string Log)> RunAsync(string ffmpegPath, IReadOnlyList<string> args, CancellationToken ct)
    {
        using var process = Process.Start(StartInfo(ffmpegPath, args, redirectOutput: false))
            ?? throw new IOException("não foi possível iniciar o FFmpeg");
        var log = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        return (process.ExitCode, log);
    }

    private static ProcessStartInfo StartInfo(string ffmpegPath, IReadOnlyList<string> args, bool redirectOutput)
    {
        var info = new ProcessStartInfo(ffmpegPath)
        {
            RedirectStandardOutput = redirectOutput,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in args)
        {
            info.ArgumentList.Add(arg);
        }
        return info;
    }

    [GeneratedRegex(@"Video: .*?, (\d{3,5})x(\d{3,5})")]
    private static partial Regex SizeRegex();
}
