namespace TelaDois.LatencyBench;

/// <summary>
/// As amostras do cenário parado separadas: <see cref="Idle"/> = a tela estava parada por ≥ 1 s antes da troca (o
/// <c>ddagrab</c> só repetia a imagem); <see cref="Active"/> = alguém mexia naquele monitor (navegador, cursor). Com
/// mediana e p95 de cada. Sem <see cref="HasIdleReference"/> (nenhuma imagem pequena na rodada) nada é classificado.
/// </summary>
public sealed record StillSplit(int Idle, int Active, double IdleMedianMs, double IdleP95Ms, double ActiveMedianMs, double ActiveP95Ms)
{
    public bool HasIdleReference { get; init; } = true;

    /// <summary>As ociosas por fase em relação à imagem completa (<see cref="StillSamples.ByPhase"/>).</summary>
    public IReadOnlyList<PhaseBucket> Phases { get; init; } = [];
}

/// <summary>
/// Uma troca do parado: índice da imagem onde apareceu, atraso, se veio de tela ociosa (null = sem referência ociosa) e
/// quantas imagens desde a última completa (a fase em relação ao GOP; null sem completa antes).
/// </summary>
public sealed record StillSample(int Index, double LatencyMs, bool? Idle, int? FramesSinceKeyframe);

/// <summary>Faixa de fase (imagens desde a última completa) com as amostras ociosas dela.</summary>
public sealed record PhaseBucket(string Label, int Count, double MedianMs);

/// <summary>Intervalo entre as trocas do parado: 2 s + 0–500 ms sorteados (semente no relatório, para repetir a rodada).</summary>
public static class StillSchedule
{
    public static readonly TimeSpan Base = TimeSpan.FromSeconds(2);

    public const int MaxJitterMs = 500;

    /// <summary>
    /// O próximo intervalo a cada chamada. O sorteio evita que a troca caia sempre na mesma fase do GOP (de 2 s: com
    /// trocas a cada 2 s exatos, ela caía sempre ~8 imagens depois da imagem completa).
    /// </summary>
    public static Func<TimeSpan> Create(int seed)
    {
        var random = new Random(seed);
        return () => Base + TimeSpan.FromMilliseconds(random.Next(0, MaxJitterMs + 1));
    }
}

public static class StillSamples
{
    /// <summary>Quanto tempo de tela parada antes da troca faz a amostra "ociosa".</summary>
    public static readonly TimeSpan IdleWindow = TimeSpan.FromSeconds(1);

    /// <summary>Folga, em bytes, sobre o tamanho mínimo da rodada (uma imagem repetida varia poucos bytes).</summary>
    public const int ToleranceBytes = 32;

    /// <summary>
    /// Teto da referência ociosa: se a menor imagem da rodada passa disso, ninguém deixou a tela parada (uma imagem
    /// repetida tem ~60 B nestes tamanhos) e não dá para classificar.
    /// </summary>
    public const int MaxIdleFrameBytes = 200;

    /// <summary>
    /// Máximo de imagens de "cauda" depois de cada completa que não contam: o <c>h264_qsv</c> refina a qualidade nelas
    /// (medido em 2026-10-21 com a tela parada: completa, 126 B, 71 B, depois 60 B). A cauda acaba na 1ª imagem que já
    /// está no tamanho mínimo.
    /// </summary>
    public const int MaxTailFrames = 5;

    /// <summary>
    /// A troca na imagem <paramref name="index"/> veio de tela parada: todas as imagens do último
    /// <see cref="IdleWindow"/> antes dela têm no máximo <paramref name="minBytes"/> + <see cref="ToleranceBytes"/>, sem
    /// contar as completas nem a cauda de cada uma. Sem <see cref="IdleWindow"/> de história, conta como com atividade.
    /// </summary>
    public static bool IsIdleBefore(IReadOnlyList<Arrival> arrivals, int index, int minBytes, long ticksPerSecond)
    {
        if (index <= 0 || index >= arrivals.Count)
        {
            return false;
        }
        var from = arrivals[index].Ticks - (long)(IdleWindow.TotalSeconds * ticksPerSecond);
        if (arrivals[0].Ticks > from)
        {
            return false;
        }
        var limit = minBytes + ToleranceBytes;
        var tail = 0;
        for (var j = 0; j < index; j++)
        {
            var frame = arrivals[j];
            if (frame.Keyframe)
            {
                tail = MaxTailFrames;
                continue;
            }
            var inTail = tail > 0 && frame.Bytes > limit;
            tail = inTail ? tail - 1 : 0;
            if (frame.Ticks >= from && !inTail && frame.Bytes > limit)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>Imagens desde a última completa até <paramref name="index"/> (0 = é a completa); null sem completa antes.</summary>
    public static int? FramesSinceKeyframe(IReadOnlyList<Arrival> arrivals, int index)
    {
        for (var j = Math.Min(index, arrivals.Count - 1); j >= 0; j--)
        {
            if (arrivals[j].Keyframe)
            {
                return index - j;
            }
        }
        return null;
    }

    /// <summary>
    /// As trocas legíveis de <see cref="LatencyMatcher.FirstChangesOnly"/>, cada uma com atraso, ociosa ou não e fase.
    /// O tamanho mínimo é o menor das imagens não completas da rodada; acima de <see cref="MaxIdleFrameBytes"/> não há
    /// referência e <see cref="StillSample.Idle"/> fica null. As três listas estão alinhadas por índice (casa pelo menor).
    /// </summary>
    public static IReadOnlyList<StillSample> Classify(
        IReadOnlyList<Arrival> arrivals, IReadOnlyList<int?> codes, IReadOnlyList<double?> latencies, long ticksPerSecond)
    {
        var count = Math.Min(arrivals.Count, Math.Min(codes.Count, latencies.Count));
        var partial = arrivals.Take(count).Where(a => !a.Keyframe).Select(a => a.Bytes).ToList();
        int? minBytes = partial.Count > 0 && partial.Min() <= MaxIdleFrameBytes ? partial.Min() : null;
        var samples = new List<StillSample>();
        foreach (var i in LatencyMatcher.FirstChangeIndices(codes, count))
        {
            if (latencies[i] is not { } latency)
            {
                continue;
            }
            bool? idle = minBytes is { } min ? IsIdleBefore(arrivals, i, min, ticksPerSecond) : null;
            samples.Add(new StillSample(i, latency, idle, FramesSinceKeyframe(arrivals, i)));
        }
        return samples;
    }

    /// <summary>Contagem, mediana e p95 das ociosas e das com atividade (<see cref="Classify"/>).</summary>
    public static StillSplit Split(
        IReadOnlyList<Arrival> arrivals, IReadOnlyList<int?> codes, IReadOnlyList<double?> latencies, long ticksPerSecond) =>
        Summarize(Classify(arrivals, codes, latencies, ticksPerSecond),
            hasReference: arrivals.Take(Math.Min(arrivals.Count, Math.Min(codes.Count, latencies.Count)))
                .Where(a => !a.Keyframe).Select(a => a.Bytes).DefaultIfEmpty(int.MaxValue).Min() <= MaxIdleFrameBytes);

    /// <summary><see cref="StillSplit"/> a partir de amostras já classificadas.</summary>
    public static StillSplit Summarize(IReadOnlyList<StillSample> samples, bool hasReference)
    {
        if (!hasReference)
        {
            return new StillSplit(0, 0, 0, 0, 0, 0) { HasIdleReference = false };
        }
        var idle = samples.Where(s => s.Idle == true).Select(s => s.LatencyMs).ToList();
        var active = samples.Where(s => s.Idle == false).Select(s => s.LatencyMs).ToList();
        var (idleMedian, idleP95, _) = BenchStats.Summarize(idle);
        var (activeMedian, activeP95, _) = BenchStats.Summarize(active);
        return new StillSplit(idle.Count, active.Count, idleMedian, idleP95, activeMedian, activeP95) { Phases = ByPhase(samples) };
    }

    /// <summary>
    /// As ociosas por faixa de fase de 30 imagens (meio segundo) desde a última completa: "0–29", "30–59", …, "≥120".
    /// Só faixas com amostras, em ordem.
    /// </summary>
    public static IReadOnlyList<PhaseBucket> ByPhase(IReadOnlyList<StillSample> samples) =>
        samples
            .Where(s => s.Idle == true && s.FramesSinceKeyframe is not null)
            .GroupBy(s => Math.Min(s.FramesSinceKeyframe!.Value / 30, 4))
            .OrderBy(g => g.Key)
            .Select(g => new PhaseBucket(
                g.Key == 4 ? "≥120" : $"{g.Key * 30}–{g.Key * 30 + 29}",
                g.Count(),
                BenchStats.Summarize(g.Select(s => s.LatencyMs)).Median))
            .ToList();
}
