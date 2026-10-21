using TelaDois.Core.Video;

namespace TelaDois.LatencyBench;

/// <summary>Uma configuração de captura/compressão a medir: perfil de compressão + ajustes.</summary>
public sealed record BenchVariant(string Name, string ProfileId, EncodeSettings Settings);

public static class BenchVariants
{
    /// <summary>As variantes da bancada, na ordem do relatório. <c>base</c> é a linha de hoje.</summary>
    public static IReadOnlyList<BenchVariant> All { get; } =
    [
        new("base", "qsv-hwmap", EncodeSettings.Default),
        new("lowpower", "qsv-hwmap", EncodeSettings.Default with
        {
            CodecOptionOverrides = new Dictionary<string, string> { ["-low_power"] = "1" },
        }),
        new("veryfast", "qsv-hwmap", EncodeSettings.Default with
        {
            CodecOptionOverrides = new Dictionary<string, string> { ["-preset"] = "veryfast" },
        }),
        new("cbr", "qsv-hwmap", EncodeSettings.Default with { Bitrate = "12M", Maxrate = "12M", Bufsize = "200k" }),
        new("intrarefresh", "qsv-hwmap", EncodeSettings.Default with
        {
            ExtraCodecOptions = ["-int_ref_type", "vertical", "-int_ref_cycle_size", "60"],
            GopSeconds = 10,
        }),
        new("gop4", "qsv-hwmap", EncodeSettings.Default with { GopSeconds = 4 }),
        new("nodup", "qsv-hwmap", EncodeSettings.Default with { GrabOptions = ":dup_frames=0" }),
        new("download", "qsv-download", EncodeSettings.Default),
        new("scale1920", "qsv-hwmap", EncodeSettings.Default with { ScaleTo = (1920, 886) }),
        new("scale2of3", "qsv-hwmap", EncodeSettings.Default with { ScaleTo = (1704, 786) }),
    ];
}
