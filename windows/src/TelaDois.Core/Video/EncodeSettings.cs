namespace TelaDois.Core.Video;

/// <summary>Como a captura e a compressão são montadas. <see cref="Default"/> = a linha de hoje.</summary>
public sealed record EncodeSettings
{
    public static EncodeSettings Default { get; } = new();

    /// <summary>Segundos entre imagens completas: <c>-g</c> = fps × GopSeconds.</summary>
    public int GopSeconds { get; init; } = 2;

    public string Bitrate { get; init; } = "12M";

    public string Maxrate { get; init; } = "20M";

    public string Bufsize { get; init; } = "4M";

    /// <summary>Opções extras do ddagrab, ex. ":dup_frames=0" (começa com ':' ou vazio).</summary>
    public string GrabOptions { get; init; } = "";

    /// <summary>Opções extras da compressão, acrescentadas depois das do perfil (ex. ["-int_ref_type","vertical"]).</summary>
    public IReadOnlyList<string> ExtraCodecOptions { get; init; } = [];

    /// <summary>Opções do perfil a trocar: chave → valor novo (ex. {"-low_power":"1"}); chave ausente = acrescenta.</summary>
    public IReadOnlyDictionary<string, string> CodecOptionOverrides { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Comprimir menor: tamanho alvo (arredondado para baixo para par). null = tamanho do monitor (cortado para par se o
    /// perfil corta). Perfis sem filtro (<c>amf-d3d11</c>, <c>nvenc-d3d11</c>) ignoram este valor.
    /// </summary>
    public (int Width, int Height)? ScaleTo { get; init; }
}
