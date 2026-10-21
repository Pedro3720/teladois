namespace TelaDois.LatencyBench;

/// <summary>
/// Marcador de 16 bits desenhado pela bancada: 4×4 blocos de 16×16 pixels, preto/branco. O bloco 0 (canto superior
/// esquerdo) é o bit mais alto; a ordem é linha por linha. Branco = bit 1.
/// </summary>
public static class MarkerCode
{
    public const int Size = 64;
    public const int Grid = 4;

    /// <summary>Distância do marcador ao canto superior esquerdo do monitor, nos dois eixos (múltiplo de 16).</summary>
    public const int Offset = 16;

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
    /// Onde o marcador fica na imagem comprimida (<paramref name="encodedWidth"/>×<paramref name="encodedHeight"/>, que
    /// pode ser menor que o monitor): posição e tamanho proporcionais, arredondados, sem passar da borda.
    /// </summary>
    public static (int X, int Y, int Width, int Height) CropIn(int monitorWidth, int monitorHeight, int encodedWidth, int encodedHeight)
    {
        var sx = (double)encodedWidth / monitorWidth;
        var sy = (double)encodedHeight / monitorHeight;
        var x = (int)Math.Round(Offset * sx);
        var y = (int)Math.Round(Offset * sy);
        var w = Math.Min((int)Math.Round(Size * sx), encodedWidth - x);
        var h = Math.Min((int)Math.Round(Size * sy), encodedHeight - y);
        return (x, y, w, h);
    }

    /// <summary>
    /// Lê o código de uma imagem cinza <paramref name="width"/>×<paramref name="height"/> (o recorte do marcador, que
    /// pode vir reduzido: usa coordenadas proporcionais). Cada bloco vale a média 3×3 em volta do centro (o ruído da
    /// compressão num pixel não estraga a leitura; blocos com menos de 8 px usam só o centro). Null se a média de algum
    /// bloco estiver entre 64 e 192 (borrado demais) ou se a imagem não servir.
    /// </summary>
    public static int? Decode(ReadOnlySpan<byte> gray, int width, int height)
    {
        if (width < Grid || height < Grid || gray.Length < (long)width * height)
        {
            return null;
        }

        var radius = Math.Min(width, height) / Grid >= 8 ? 1 : 0;
        var code = 0;
        for (var row = 0; row < Grid; row++)
        {
            var y = (2 * row + 1) * height / (2 * Grid);
            for (var col = 0; col < Grid; col++)
            {
                var x = (2 * col + 1) * width / (2 * Grid);
                var value = Average(gray, width, height, x, y, radius);
                if (value >= BlurLow && value <= BlurHigh)
                {
                    return null;
                }
                code = (code << 1) | (value >= Threshold ? 1 : 0);
            }
        }
        return code;
    }

    private static int Average(ReadOnlySpan<byte> gray, int width, int height, int cx, int cy, int radius)
    {
        int sum = 0, count = 0;
        for (var y = Math.Max(0, cy - radius); y <= Math.Min(height - 1, cy + radius); y++)
        {
            for (var x = Math.Max(0, cx - radius); x <= Math.Min(width - 1, cx + radius); x++)
            {
                sum += gray[y * width + x];
                count++;
            }
        }
        return sum / count;
    }
}
