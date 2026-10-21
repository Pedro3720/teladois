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
}

public static class BenchStats
{
    public static string CsvHeader =>
        "variant,scenario,median_ms,p95_ms,max_ms,fps,gaps_over_33ms,gaps_over_50ms,avg_keyframe_kb,max_keyframe_kb,mbps,measured,missing,suspect";

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
        var sorted = latencies.Where(l => l.HasValue).Select(l => l!.Value).Order().ToArray();
        var missing = latencies.Count - sorted.Length;

        double median = 0, p95 = 0, max = 0;
        if (sorted.Length > 0)
        {
            var n = sorted.Length;
            median = n % 2 == 1 ? sorted[n / 2] : (sorted[n / 2 - 1] + sorted[n / 2]) / 2.0;
            p95 = sorted[(int)Math.Ceiling(0.95 * n) - 1];
            max = sorted[n - 1];
        }

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

        return new BenchResult(variant, scenario, median, p95, max, fps, over33, over50, avgKey, maxKey, mbps, sorted.Length, missing);
    }

    /// <summary>Uma linha do CSV, com ponto decimal (cultura invariante). Variante e cenário não levam vírgula.</summary>
    public static string ToCsv(BenchResult r)
    {
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
            r.Suspect ? "1" : "0");
    }
}
