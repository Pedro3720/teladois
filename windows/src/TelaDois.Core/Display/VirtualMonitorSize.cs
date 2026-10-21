namespace TelaDois.Core.Display;

/// <summary>
/// Tamanho do monitor virtual para um iPhone (Etapa 5d, §7 do spec): a resolução do iPhone ÷ 1,75, cada lado no par mais
/// próximo. Em escala 100% o texto fica do tamanho que o usuário via em 175% no tamanho cheio, o Windows não mistura
/// escalas entre monitores (programas antigos, sem DPI por monitor, andam por todas as telas) e cada imagem tem ~1/3
/// dos pixels. O <c>vdd_settings.xml</c> tem esses modos.
/// </summary>
public static class VirtualMonitorSize
{
    /// <summary>
    /// (<paramref name="iphoneWidth"/> ÷ 1,75, <paramref name="iphoneHeight"/> ÷ 1,75), cada um no par mais próximo
    /// (2556×1179 → 1460×674); nunca menos de 2 (um HELLO absurdo não vira tamanho 0).
    /// </summary>
    public static (int Width, int Height) For(int iphoneWidth, int iphoneHeight) => (Side(iphoneWidth), Side(iphoneHeight));

    // x ÷ 1,75 no par mais próximo = 2 × round(2x/7); em inteiros, round(2x/7) = (4x + 7) / 14 (2x/7 nunca termina em ,5).
    private static int Side(int iphone) => iphone <= 0 ? 2 : (int)Math.Max(2, (4L * iphone + 7) / 14 * 2);
}
