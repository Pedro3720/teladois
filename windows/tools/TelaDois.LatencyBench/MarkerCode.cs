namespace TelaDois.LatencyBench;

/// <summary>
/// Marcador de 16 bits desenhado pela bancada: 4×4 blocos de 16×16 pixels, preto/branco. O bloco 0 (canto superior
/// esquerdo) é o bit mais alto; a ordem é linha por linha. Branco = bit 1.
/// </summary>
public static class MarkerCode
{
    public const int Size = 64;
    public const int Grid = 4;

    private const int BlurLow = 64;
    private const int BlurHigh = 192;
    private const int Threshold = 128;

    /// <summary>Os 16 blocos (true = branco) de <paramref name="code"/> (só os 16 bits de baixo contam).</summary>
    public static bool[] Encode(int code)
    {
        var blocks = new bool[Grid * Grid];
        for (var i = 0; i < blocks.Length; i++)
        {
            blocks[i] = ((code >> (blocks.Length - 1 - i)) & 1) != 0;
        }
        return blocks;
    }

    /// <summary>
    /// Lê o código de uma imagem cinza <paramref name="width"/>×<paramref name="height"/> (o recorte do marcador, que
    /// pode vir reduzido: usa coordenadas proporcionais). Null se algum centro de bloco estiver entre 64 e 192
    /// (borrado demais) ou se a imagem não servir.
    /// </summary>
    public static int? Decode(ReadOnlySpan<byte> gray, int width, int height)
    {
        if (width < Grid || height < Grid || gray.Length < (long)width * height)
        {
            return null;
        }

        var code = 0;
        for (var row = 0; row < Grid; row++)
        {
            var y = (2 * row + 1) * height / (2 * Grid);
            for (var col = 0; col < Grid; col++)
            {
                var x = (2 * col + 1) * width / (2 * Grid);
                int value = gray[y * width + x];
                if (value >= BlurLow && value <= BlurHigh)
                {
                    return null;
                }
                code = (code << 1) | (value >= Threshold ? 1 : 0);
            }
        }
        return code;
    }
}
