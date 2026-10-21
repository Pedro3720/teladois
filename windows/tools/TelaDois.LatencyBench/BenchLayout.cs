namespace TelaDois.LatencyBench;

/// <summary>Um retângulo em pixels, relativo ao canto superior esquerdo do monitor (ou da imagem comprimida).</summary>
public readonly record struct PixelRect(int X, int Y, int Width, int Height);

/// <summary>
/// Onde as janelas da bancada ficam no monitor medido e onde elas caem na imagem comprimida (que pode ser menor).
/// Tudo relativo ao canto superior esquerdo do monitor.
/// </summary>
public static class BenchLayout
{
    /// <summary>O marcador de 64×64 em (16, 16).</summary>
    public static PixelRect Marker => new(MarkerCode.Offset, MarkerCode.Offset, MarkerCode.Size, MarkerCode.Size);

    /// <summary>As cenas começam depois do marcador: 16 + 64 + 16.</summary>
    public const int SceneLeft = MarkerCode.Offset * 2 + MarkerCode.Size;

    /// <summary>Rolagem: 1/3 da largura × altura toda, ao lado do marcador.</summary>
    public static PixelRect Scroll(int monitorWidth, int monitorHeight) => new(SceneLeft, 0, monitorWidth / 3, monitorHeight);

    /// <summary>Vídeo: 1/2 × 1/2, ao lado do marcador.</summary>
    public static PixelRect Noise(int monitorWidth, int monitorHeight) =>
        new(SceneLeft, MarkerCode.Offset, monitorWidth / 2, monitorHeight / 2);

    /// <summary>
    /// <paramref name="rect"/> do monitor (<paramref name="monitorWidth"/>×<paramref name="monitorHeight"/>) na imagem
    /// comprimida (<paramref name="encodedWidth"/>×<paramref name="encodedHeight"/>): proporcional, arredondado, sem passar da borda.
    /// </summary>
    public static PixelRect Scale(PixelRect rect, int monitorWidth, int monitorHeight, int encodedWidth, int encodedHeight)
    {
        var sx = (double)encodedWidth / monitorWidth;
        var sy = (double)encodedHeight / monitorHeight;
        var x = Math.Min((int)Math.Round(rect.X * sx), encodedWidth);
        var y = Math.Min((int)Math.Round(rect.Y * sy), encodedHeight);
        var w = Math.Min((int)Math.Round(rect.Width * sx), encodedWidth - x);
        var h = Math.Min((int)Math.Round(rect.Height * sy), encodedHeight - y);
        return new PixelRect(x, y, w, h);
    }

    /// <summary>
    /// Filtro (<c>-filter_complex</c>) que tira a imagem <paramref name="index"/> e mostra <b>só</b> as janelas da bancada:
    /// o marcador (completado com preto até a altura da rolagem) ao lado da janela de rolagem. Nada do resto da tela entra.
    /// </summary>
    public static string ScrollPngFilter(int index, int monitorWidth, int monitorHeight, int encodedWidth, int encodedHeight)
    {
        var m = Scale(Marker, monitorWidth, monitorHeight, encodedWidth, encodedHeight);
        var s = Scale(Scroll(monitorWidth, monitorHeight), monitorWidth, monitorHeight, encodedWidth, encodedHeight);
        return $"[0:v]select=eq(n\\,{index}),format=rgb24,split=2[a][b];" +
            $"[a]crop={m.Width}:{m.Height}:{m.X}:{m.Y},pad={m.Width}:{Math.Max(s.Height, m.Height)}:0:0:black[m];" +
            $"[b]crop={s.Width}:{s.Height}:{s.X}:{s.Y}[s];" +
            "[m][s]hstack=inputs=2";
    }
}
