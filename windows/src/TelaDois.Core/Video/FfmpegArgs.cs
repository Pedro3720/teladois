using System.Globalization;
using TelaDois.Core.Link;

namespace TelaDois.Core.Video;

/// <summary>Linhas de comando do FFmpeg (testadas sem rodar o FFmpeg).</summary>
public static class FfmpegArgs
{
    public static IReadOnlyList<string> Desktop(int outputIndex, VideoConfig config, string encoder)
    {
        var fps = config.Fps.ToString(CultureInfo.InvariantCulture);
        // O h264_amf recusa BGRA e o scale_d3d11 falha neste driver: converte para NV12 na CPU.
        var filter = $"ddagrab=output_idx={outputIndex}:framerate={fps}:draw_mouse=1,hwdownload,format=bgra,format=nv12";
        return ["-hide_banner", "-loglevel", "error", "-filter_complex", filter, "-an", .. Encoder(encoder, fps), "-f", "h264", "-"];
    }

    public static IReadOnlyList<string> TestPattern(VideoConfig config, int frames)
    {
        var fps = config.Fps.ToString(CultureInfo.InvariantCulture);
        var size = $"{config.Width}x{config.Height}";
        return ["-hide_banner", "-loglevel", "error", "-f", "lavfi", "-i", $"testsrc2=size={size}:rate={fps}",
            "-frames:v", frames.ToString(CultureInfo.InvariantCulture), "-pix_fmt", "yuv420p", .. Encoder("libx264", fps), "-f", "h264", "-"];
    }

    /// <summary>Abre a saída <paramref name="outputIndex"/> por um quadro, para ler a resolução no log.</summary>
    public static IReadOnlyList<string> ProbeOutput(int outputIndex) =>
        ["-hide_banner", "-filter_complex", $"ddagrab=output_idx={outputIndex}", "-frames:v", "1", "-f", "null", "-"];

    private static IReadOnlyList<string> Encoder(string encoder, string fps) => encoder switch
    {
        "h264_amf" => ["-c:v", "h264_amf", "-usage", "ultralowlatency", "-bf", "0", "-b:v", "12M", "-g", fps, "-header_spacing", fps],
        _ => ["-c:v", "libx264", "-preset", "ultrafast", "-tune", "zerolatency", "-profile:v", "main", "-bf", "0", "-b:v", "12M",
            "-g", fps, "-x264-params", "repeat-headers=1", "-bsf:v", "h264_metadata=aud=insert"],
    };
}
