namespace TelaDois.LatencyBench;

/// <summary>
/// As amostras do cenário parado separadas: <see cref="Idle"/> = a tela estava ociosa antes da troca (o <c>ddagrab</c> só
/// repetia a imagem); <see cref="Active"/> = alguém mexia naquele monitor (navegador, cursor). Com mediana e p95 de cada.
/// </summary>
public sealed record StillSplit(int Idle, int Active, double IdleMedianMs, double IdleP95Ms, double ActiveMedianMs, double ActiveP95Ms);

public static class StillSamples
{
    /// <summary>Quantas imagens antes da troca precisam estar no tamanho mínimo para a amostra ser "ociosa".</summary>
    public const int Lookback = 10;

    /// <summary>Folga, em bytes, sobre o tamanho mínimo da rodada (uma imagem repetida varia poucos bytes).</summary>
    public const int ToleranceBytes = 32;

    /// <summary>
    /// Imagens logo depois de cada completa que também não contam: o <c>h264_qsv</c> refina a qualidade nelas (medido em
    /// 2026-10-21 com a tela parada: completa, 126 B, 71 B, depois 60 B). Com GOP de 2 s e trocas a cada 2 s, a completa
    /// cai sempre ~8 imagens antes da troca, e essas duas fariam toda troca parecer "com atividade".
    /// </summary>
    public const int FramesAfterKeyframeIgnored = 2;

    /// <summary>
    /// A troca na imagem <paramref name="index"/> veio de tela ociosa: as <see cref="Lookback"/> imagens antes dela têm no
    /// máximo <paramref name="minBytes"/> + <see cref="ToleranceBytes"/>, sem contar as completas (vêm a cada 2 s de
    /// qualquer jeito) nem as <see cref="FramesAfterKeyframeIgnored"/> seguintes a cada uma. Sem <see cref="Lookback"/>
    /// imagens antes, não dá para saber: conta como com atividade.
    /// </summary>
    public static bool IsIdleBefore(IReadOnlyList<Arrival> arrivals, int index, int minBytes)
    {
        if (index < Lookback || index > arrivals.Count)
        {
            return false;
        }
        for (var j = index - Lookback; j < index; j++)
        {
            if (!NearKeyframe(arrivals, j) && arrivals[j].Bytes > minBytes + ToleranceBytes)
            {
                return false;
            }
        }
        return true;
    }

    private static bool NearKeyframe(IReadOnlyList<Arrival> arrivals, int j)
    {
        for (var k = Math.Max(0, j - FramesAfterKeyframeIgnored); k <= j; k++)
        {
            if (arrivals[k].Keyframe)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Separa as amostras de <see cref="LatencyMatcher.FirstChangesOnly"/> (as legíveis) em ociosas e com atividade. O
    /// tamanho mínimo é o menor das imagens não completas da rodada. <paramref name="arrivals"/>,
    /// <paramref name="codes"/> e <paramref name="latencies"/> estão alinhados por índice (casa pelo menor).
    /// </summary>
    public static StillSplit Split(IReadOnlyList<Arrival> arrivals, IReadOnlyList<int?> codes, IReadOnlyList<double?> latencies)
    {
        var count = Math.Min(arrivals.Count, Math.Min(codes.Count, latencies.Count));
        var partial = arrivals.Take(count).Where(a => !a.Keyframe).ToList();
        if (partial.Count == 0)
        {
            return new StillSplit(0, 0, 0, 0, 0, 0);
        }
        var minBytes = partial.Min(a => a.Bytes);
        var idle = new List<double>();
        var active = new List<double>();
        foreach (var i in LatencyMatcher.FirstChangeIndices(codes, count))
        {
            if (latencies[i] is not { } latency)
            {
                continue;
            }
            (IsIdleBefore(arrivals, i, minBytes) ? idle : active).Add(latency);
        }
        var (idleMedian, idleP95, _) = BenchStats.Summarize(idle);
        var (activeMedian, activeP95, _) = BenchStats.Summarize(active);
        return new StillSplit(idle.Count, active.Count, idleMedian, idleP95, activeMedian, activeP95);
    }
}
