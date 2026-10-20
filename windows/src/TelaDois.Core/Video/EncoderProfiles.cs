namespace TelaDois.Core.Video;

/// <summary>Uma compressão H.264 e como ligar a captura (superfícies D3D11 do ddagrab) a ela.</summary>
public sealed record EncoderProfile(string Id, string Codec, string Filter, IReadOnlyList<string> Options)
{
    /// <summary>
    /// Tamanho que sai do FFmpeg para um monitor de <paramref name="width"/>×<paramref name="height"/> (é o que o
    /// CONFIG informa): par quando o filtro corta para par; senão, o do monitor.
    /// </summary>
    public (int Width, int Height) EncodedSize(int width, int height) =>
        Filter.Contains(EncoderProfiles.EvenCrop, StringComparison.Ordinal) || Filter.Contains(EncoderProfiles.QsvEvenCrop, StringComparison.Ordinal)
            ? (width & ~1, height & ~1)
            : (width, height);
}

public static class EncoderProfiles
{
    /// <summary>
    /// Corta para largura e altura pares (no máximo 1 coluna e 1 linha, invisível no iPhone). O monitor virtual tem a
    /// resolução do iPhone (2556×1179): x264 ("height not divisible by 2") e Media Foundation (MF_E_INVALIDMEDIATYPE)
    /// recusam altura ímpar (medido em 2026-10-20). Vai antes da conversão de formato, ainda em BGRA.
    /// </summary>
    public const string EvenCrop = "crop=trunc(iw/2)*2:trunc(ih/2)*2";

    /// <summary>
    /// O mesmo corte na própria placa Intel. Sem ele o h264_qsv aceita 1179, mas codifica 1180 (arredonda para cima,
    /// medido); o <c>crop</c> comum não serve aqui (com superfícies da placa o hwmap ignora o corte, medido).
    /// </summary>
    public const string QsvEvenCrop = "vpp_qsv=cw=trunc(iw/2)*2:ch=trunc(ih/2)*2";

    private const string Download = "hwdownload,format=bgra," + EvenCrop;

    /// <summary>
    /// Ordem da spec. Validados no PC Intel em 2026-10-19: qsv-hwmap, qsv-download, mf, x264.
    /// AMF e NVENC (d3d11 direto e com download) são validados no PC de casa (Task 8). <c>amf-d3d11</c> e
    /// <c>nvenc-d3d11</c> ficam sem corte: não há placa AMD/NVIDIA aqui para provar um (se falharem com tamanho ímpar,
    /// a sondagem cai no perfil com download, que corta).
    /// </summary>
    public static IReadOnlyList<EncoderProfile> Candidates { get; } =
    [
        new("amf-d3d11", "h264_amf", "", ["-usage", "ultralowlatency"]),
        new("amf-download", "h264_amf", Download + ",format=nv12", []),
        new("qsv-hwmap", "h264_qsv", "hwmap=derive_device=qsv,format=qsv," + QsvEvenCrop, ["-low_power", "0", "-look_ahead", "0", "-async_depth", "1"]),
        new("qsv-download", "h264_qsv", Download + ",format=nv12", ["-async_depth", "1"]),
        new("nvenc-d3d11", "h264_nvenc", "", ["-preset", "p1", "-tune", "ull"]),
        new("nvenc-download", "h264_nvenc", Download + ",format=nv12", ["-preset", "p1", "-tune", "ull"]),
        new("mf", "h264_mf", Download + ",format=nv12", ["-rate_control", "cbr", "-scenario", "display_remoting"]),
        new("x264", "libx264", Download + ",format=yuv420p", ["-preset", "ultrafast", "-tune", "zerolatency"]),
    ];
}
