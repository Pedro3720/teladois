using TelaDois.Core.Video;

namespace TelaDois.LatencyBench;

/// <summary>
/// Uma configuração de captura/compressão a medir: perfil de compressão + ajustes + imagens por segundo pedidas ao
/// ddagrab (<c>framerate</c>; o <c>-g</c> segue, para a imagem completa continuar a cada <see cref="EncodeSettings.GopSeconds"/>).
/// </summary>
public sealed record BenchVariant(string Name, string ProfileId, EncodeSettings Settings, ushort Fps = BenchVariant.DefaultFps)
{
    /// <summary>A taxa do app (o monitor virtual é 60 Hz).</summary>
    public const ushort DefaultFps = 60;
}

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

        // Rodada 2 (2026-10-21): as duas que mais ajudaram na 1ª matriz, juntas.
        new("veryfast-scale1920", "qsv-hwmap", EncodeSettings.Default with
        {
            CodecOptionOverrides = new Dictionary<string, string> { ["-preset"] = "veryfast" },
            ScaleTo = (1920, 886),
        }),
        new("veryfast-scale2of3", "qsv-hwmap", EncodeSettings.Default with
        {
            CodecOptionOverrides = new Dictionary<string, string> { ["-preset"] = "veryfast" },
            ScaleTo = (1704, 786),
        }),

        // Diagnósticos do parado (~300 ms em todas as variantes QSV da 1ª matriz). x264 e mf trocam só a compressão
        // (mesma captura com download que a variante "download"): se o parado continuar ~300 ms com eles, a demora não é
        // do h264_qsv.
        new("x264", "x264", EncodeSettings.Default),
        new("mf", "mf", EncodeSettings.Default),

        // Diagnóstico do lado da captura: ddagrab a 30/s (o -g cai para 60 = os mesmos 2 s). Hipótese: se o atraso do
        // parado for uma fila de N imagens parada em algum ponto do FFmpeg (captura → filtro → compressão), a 30/s ele
        // dobra (~600 ms); se for um tempo fixo fora do FFmpeg (DWM ou o driver do monitor virtual acordando depois da
        // tela parada), fica nos ~300 ms. Nos cenários com movimento o fps cai para ~30 e quase todo intervalo passa de
        // 33 ms: ali só a mediana e o p95 interessam.
        new("fps30", "qsv-hwmap", EncodeSettings.Default, Fps: 30),
    ];
}
