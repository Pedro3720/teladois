using System.Globalization;

namespace TelaDois.LatencyBench;

public sealed record BenchResult(
    string Variant,
    string Scenario,
    double MedianMs,
    double P95Ms,
    double MaxMs,
    double Fps,
    int GapsOver33,
    int GapsOver50,
    double AvgKeyframeKB,
    double MaxKeyframeKB,
    double Mbps,
    int Measured,
    int Missing)
{
    /// <summary>Rodada suspeita: o número de imagens descomprimidas não bateu com o de chegadas (casou pelo menor).</summary>
    public bool Suspect { get; init; }

    /// <summary>Só no cenário parado: as amostras separadas em "ociosa" e "com atividade" (<see cref="StillSamples"/>).</summary>
    public StillSplit? Still { get; init; }

    /// <summary>
    /// "Atraso com leitor ideal": o mesmo casamento, mas com a chegada no instante em que o último byte da imagem foi
    /// lido do FFmpeg (o <c>AccessUnitReader</c> só a entrega quando chega o AUD da seguinte). Null se não deu para medir.
    /// </summary>
    public double? IdealMedianMs { get; init; }

    public double? IdealP95Ms { get; init; }

    /// <summary>Só no parado: a separação ociosa × com atividade com o atraso do leitor ideal.</summary>
    public StillSplit? IdealStill { get; init; }
}

public static class BenchStats
{
    public static string CsvHeader =>
        "variant,scenario,median_ms,p95_ms,max_ms,fps,gaps_over_33ms,gaps_over_50ms,avg_keyframe_kb,max_keyframe_kb,mbps,measured,missing,suspect,idle_samples,idle_median_ms,idle_p95_ms,active_samples,active_median_ms,active_p95_ms,ideal_median_ms,ideal_p95_ms,ideal_idle_median_ms,ideal_idle_p95_ms";

    /// <summary>
    /// Estatística de uma rodada. Atrasos: mediana (média dos dois do meio se par), p95 = elemento de índice
    /// <c>ceil(0,95 n) − 1</c> da lista ordenada, máximo. Fps e Mbps pela janela entre a 1ª e a última chegada (os bytes
    /// contados são os das chegadas depois da 1ª). Buracos = intervalos entre chegadas consecutivas acima de 33 / 50 ms.
    /// </summary>
    public static BenchResult Compute(
        string variant,
        string scenario,
        IReadOnlyList<Arrival> arrivals,
        IReadOnlyList<double?> latencies,
        long ticksPerSecond)
    {
        var values = latencies.Where(l => l.HasValue).Select(l => l!.Value).ToArray();
        var missing = latencies.Count - values.Length;
        var (median, p95, max) = Summarize(values);

        double fps = 0, mbps = 0;
        int over33 = 0, over50 = 0;
        if (arrivals.Count >= 2)
        {
            var windowSeconds = (arrivals[^1].Ticks - arrivals[0].Ticks) / (double)ticksPerSecond;
            if (windowSeconds > 0)
            {
                fps = (arrivals.Count - 1) / windowSeconds;
                long bytes = 0;
                for (var i = 1; i < arrivals.Count; i++)
                {
                    bytes += arrivals[i].Bytes;
                }
                mbps = bytes * 8.0 / 1e6 / windowSeconds;
            }

            for (var i = 1; i < arrivals.Count; i++)
            {
                var gapMs = (arrivals[i].Ticks - arrivals[i - 1].Ticks) * 1000.0 / ticksPerSecond;
                if (gapMs > 33)
                {
                    over33++;
                }
                if (gapMs > 50)
                {
                    over50++;
                }
            }
        }

        var keyframes = arrivals.Where(a => a.Keyframe).Select(a => a.Bytes / 1024.0).ToArray();
        var avgKey = keyframes.Length > 0 ? keyframes.Average() : 0;
        var maxKey = keyframes.Length > 0 ? keyframes.Max() : 0;

        return new BenchResult(variant, scenario, median, p95, max, fps, over33, over50, avgKey, maxKey, mbps, values.Length, missing);
    }

    /// <summary>
    /// Por que não dá para acrescentar linhas em <paramref name="csvPath"/>: o arquivo existe com outro cabeçalho (de uma
    /// versão anterior da bancada). Null se não existe ou se o cabeçalho é o de hoje.
    /// </summary>
    public static string? HeaderProblem(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            return null;
        }
        using var reader = new StreamReader(csvPath);
        var header = reader.ReadLine()?.Trim();
        return header == CsvHeader
            ? null
            : $"{csvPath} tem colunas de outra versão da bancada: use outra pasta com --out ou apague o arquivo antigo";
    }

    /// <summary>Mediana (média dos dois do meio se par), p95 (índice <c>ceil(0,95 n) − 1</c> da lista ordenada) e máximo; zeros sem valores.</summary>
    public static (double Median, double P95, double Max) Summarize(IEnumerable<double> values)
    {
        var sorted = values.Order().ToArray();
        var n = sorted.Length;
        if (n == 0)
        {
            return (0, 0, 0);
        }
        var median = n % 2 == 1 ? sorted[n / 2] : (sorted[n / 2 - 1] + sorted[n / 2]) / 2.0;
        return (median, sorted[(int)Math.Ceiling(0.95 * n) - 1], sorted[n - 1]);
    }

    /// <summary>Uma linha do CSV, com ponto decimal (cultura invariante). Variante e cenário não levam vírgula.</summary>
    public static string ToCsv(BenchResult r)
    {
        // Sem referência ociosa, as colunas do parado ficam vazias (nada foi classificado).
        var still = r.Still is { HasIdleReference: true } ? r.Still : null;
        var idealStill = r.IdealStill is { HasIdleReference: true } ? r.IdealStill : null;
        var c = CultureInfo.InvariantCulture;
        return string.Join(',',
            r.Variant,
            r.Scenario,
            r.MedianMs.ToString("0.0", c),
            r.P95Ms.ToString("0.0", c),
            r.MaxMs.ToString("0.0", c),
            r.Fps.ToString("0.0", c),
            r.GapsOver33.ToString(c),
            r.GapsOver50.ToString(c),
            r.AvgKeyframeKB.ToString("0.0", c),
            r.MaxKeyframeKB.ToString("0.0", c),
            r.Mbps.ToString("0.00", c),
            r.Measured.ToString(c),
            r.Missing.ToString(c),
            r.Suspect ? "1" : "0",
            still?.Idle.ToString(c) ?? "",
            still?.IdleMedianMs.ToString("0.0", c) ?? "",
            still?.IdleP95Ms.ToString("0.0", c) ?? "",
            still?.Active.ToString(c) ?? "",
            still?.ActiveMedianMs.ToString("0.0", c) ?? "",
            still?.ActiveP95Ms.ToString("0.0", c) ?? "",
            r.IdealMedianMs?.ToString("0.0", c) ?? "",
            r.IdealP95Ms?.ToString("0.0", c) ?? "",
            idealStill?.IdleMedianMs.ToString("0.0", c) ?? "",
            idealStill?.IdleP95Ms.ToString("0.0", c) ?? "");
    }
}
