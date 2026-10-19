namespace TelaDois.Core.Video;

/// <summary>Uma compressão H.264 e como ligar a captura (superfícies D3D11 do ddagrab) a ela.</summary>
public sealed record EncoderProfile(string Id, string Codec, string Filter, IReadOnlyList<string> Options);

public static class EncoderProfiles
{
    /// <summary>
    /// Ordem da spec. Validados no PC Intel em 2026-10-19: qsv-hwmap, qsv-download, mf, x264.
    /// AMF e NVENC (d3d11 direto e com download) são validados no PC de casa (Task 8).
    /// </summary>
    public static IReadOnlyList<EncoderProfile> Candidates { get; } =
    [
        new("amf-d3d11", "h264_amf", "", ["-usage", "ultralowlatency"]),
        new("amf-download", "h264_amf", "hwdownload,format=bgra,format=nv12", []),
        new("qsv-hwmap", "h264_qsv", "hwmap=derive_device=qsv,format=qsv", ["-low_power", "0", "-look_ahead", "0", "-async_depth", "1"]),
        new("qsv-download", "h264_qsv", "hwdownload,format=bgra,format=nv12", ["-async_depth", "1"]),
        new("nvenc-d3d11", "h264_nvenc", "", ["-preset", "p1", "-tune", "ull"]),
        new("nvenc-download", "h264_nvenc", "hwdownload,format=bgra,format=nv12", ["-preset", "p1", "-tune", "ull"]),
        new("mf", "h264_mf", "hwdownload,format=bgra,format=nv12", ["-rate_control", "cbr", "-scenario", "display_remoting"]),
        new("x264", "libx264", "hwdownload,format=bgra,format=yuv420p", ["-preset", "ultrafast", "-tune", "zerolatency"]),
    ];
}
