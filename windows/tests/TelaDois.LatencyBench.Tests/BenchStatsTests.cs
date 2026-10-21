using System.Globalization;
using TelaDois.LatencyBench;

namespace TelaDois.LatencyBench.Tests;

public class BenchStatsTests
{
    private const long Tps = 6000; // 100 ticks = 1/60 s

    private static List<Arrival> Steady(int count, long step, long start = 0, int bytes = 1000) =>
        Enumerable.Range(0, count).Select(i => new Arrival(start + i * step, false, bytes)).ToList();

    private static List<double?> Same(int count, double value) => Enumerable.Repeat((double?)value, count).ToList();

    [Fact]
    public void LatencyPercentilesAreExact()
    {
        // 10 atrasos (1..10 ms embaralhados) e um nulo.
        double?[] latencies = [7, 1, 10, 3, null, 5, 9, 2, 8, 4, 6];

        var r = BenchStats.Compute("v", "s", Steady(latencies.Length, 100), latencies, Tps);

        Assert.Equal(5.5, r.MedianMs, 6);
        Assert.Equal(10, r.P95Ms, 6); // índice ceil(0,95 * 10) - 1 = 9
        Assert.Equal(10, r.MaxMs, 6);
        Assert.Equal(10, r.Measured);
        Assert.Equal(1, r.Missing);
    }

    [Fact]
    public void OddCountMedianIsTheMiddleElement()
    {
        double?[] latencies = [30, 10, 20];
        var r = BenchStats.Compute("v", "s", Steady(3, 100), latencies, Tps);
        Assert.Equal(20, r.MedianMs, 6);
        Assert.Equal(30, r.P95Ms, 6);
    }

    [Fact]
    public void P95OfTwentyIsTheNineteenthValue()
    {
        var latencies = Enumerable.Range(1, 20).Select(i => (double?)i).ToList();
        var r = BenchStats.Compute("v", "s", Steady(20, 100), latencies, Tps);
        Assert.Equal(19, r.P95Ms, 6); // ceil(19) - 1 = índice 18 -> valor 19
        Assert.Equal(20, r.MaxMs, 6);
    }

    [Fact]
    public void CountsGapsAndFps()
    {
        // 60 chegadas a cada 1/60 s, um buraco de 60 ms e mais 41 chegadas.
        var list = Steady(60, 100);
        list.AddRange(Steady(41, 100, start: 5900 + 360));

        var r = BenchStats.Compute("v", "s", list, Same(list.Count, 10), Tps);

        Assert.Equal(1, r.GapsOver33);
        Assert.Equal(1, r.GapsOver50);
        Assert.InRange(r.Fps, 58.0, 60.0);
    }

    [Fact]
    public void RegularSixtyFpsHasNoGaps()
    {
        var arrivals = Steady(121, 100); // 2 s a 60/s

        var r = BenchStats.Compute("v", "s", arrivals, Same(arrivals.Count, 10), Tps);

        Assert.Equal(0, r.GapsOver33);
        Assert.Equal(0, r.GapsOver50);
        Assert.Equal(60, r.Fps, 6);
    }

    [Fact]
    public void GapBetween33And50CountsOnlyOver33()
    {
        long[] ticks = [0, 100, 340, 440]; // intervalos: 16,7 ms, 40 ms, 16,7 ms
        var arrivals = ticks.Select(t => new Arrival(t, false, 1)).ToList();

        var r = BenchStats.Compute("v", "s", arrivals, Same(4, 1), Tps);

        Assert.Equal(1, r.GapsOver33);
        Assert.Equal(0, r.GapsOver50);
    }

    [Fact]
    public void KeyframeSizesAndBitrate()
    {
        // 11 chegadas, 1 s entre cada; 2 são imagens completas (100 KB e 300 KB).
        var arrivals = new List<Arrival>();
        for (var i = 0; i < 11; i++)
        {
            var key = i == 0 || i == 5;
            var bytes = i == 0 ? 102_400 : i == 5 ? 307_200 : 125_000;
            arrivals.Add(new Arrival(i * Tps, key, bytes));
        }

        var r = BenchStats.Compute("v", "s", arrivals, Same(arrivals.Count, 5), Tps);

        Assert.Equal(200, r.AvgKeyframeKB, 6);
        Assert.Equal(300, r.MaxKeyframeKB, 6);
        // bytes depois da 1ª chegada: 9 x 125 000 + 307 200 = 1 432 200 em 10 s
        Assert.Equal(1_432_200 * 8 / 1e6 / 10, r.Mbps, 6);
        Assert.Equal(1, r.Fps, 6);
    }

    [Fact]
    public void NoKeyframesOrNoLatenciesGiveZeros()
    {
        var r = BenchStats.Compute("v", "s", Steady(3, 100), [null, null, null], Tps);

        Assert.Equal(0, r.MedianMs);
        Assert.Equal(0, r.P95Ms);
        Assert.Equal(0, r.MaxMs);
        Assert.Equal(0, r.AvgKeyframeKB);
        Assert.Equal(0, r.MaxKeyframeKB);
        Assert.Equal(0, r.Measured);
        Assert.Equal(3, r.Missing);
    }

    [Fact]
    public void FewerThanTwoArrivalsHasNoRate()
    {
        var r = BenchStats.Compute("v", "s", Steady(1, 100), [4], Tps);

        Assert.Equal(0, r.Fps);
        Assert.Equal(0, r.Mbps);
        Assert.Equal(0, r.GapsOver33);
    }

    // Valor de uma coluna do CSV pelo nome do cabeçalho.
    private static string Column(string line, string name) =>
        line.Split(',')[Array.IndexOf(BenchStats.CsvHeader.Split(','), name)];

    [Fact]
    public void CsvUsesInvariantCultureAndMatchesTheHeader()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
            var r = new BenchResult("base", "rolagem", 12.5, 30.25, 41.75, 59.5, 1, 2, 80.5, 120.25, 11.75, 90, 3);

            var line = BenchStats.ToCsv(r);

            Assert.Contains("12.5", line);
            Assert.DoesNotContain("12,5", line);
            Assert.Equal(BenchStats.CsvHeader.Split(',').Length, line.Split(',').Length);
            Assert.Equal("base", Column(line, "variant"));
            Assert.Equal("rolagem", Column(line, "scenario"));
            Assert.Equal("90", Column(line, "measured"));
            Assert.Equal("3", Column(line, "missing"));
            Assert.Equal("0", Column(line, "suspect"));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void SuspectRoundIsMarkedInTheCsv()
    {
        var r = new BenchResult("base", "rolagem", 1, 2, 3, 60, 0, 0, 1, 1, 1, 10, 0) { Suspect = true };

        Assert.Equal("1", Column(BenchStats.ToCsv(r), "suspect"));
        Assert.False(new BenchResult("base", "rolagem", 1, 2, 3, 60, 0, 0, 1, 1, 1, 10, 0).Suspect);
    }

    [Fact]
    public void StillSplitHasItsOwnColumnsEmptyOutsideTheStillScenario()
    {
        var plain = BenchStats.ToCsv(new BenchResult("base", "rolagem", 1, 2, 3, 60, 0, 0, 1, 1, 1, 10, 0));
        var still = BenchStats.ToCsv(new BenchResult("base", "parado", 1, 2, 3, 60, 0, 0, 1, 1, 1, 10, 0)
        {
            Still = new StillSplit(4, 6, 305.5, 312, 30.26, 54),
        });

        foreach (var name in new[] { "idle_samples", "idle_median_ms", "idle_p95_ms", "active_samples", "active_median_ms", "active_p95_ms" })
        {
            Assert.Equal("", Column(plain, name));
        }
        Assert.Equal("4", Column(still, "idle_samples"));
        Assert.Equal("305.5", Column(still, "idle_median_ms"));
        Assert.Equal("312.0", Column(still, "idle_p95_ms"));
        Assert.Equal("6", Column(still, "active_samples"));
        Assert.Equal("30.3", Column(still, "active_median_ms"));
        Assert.Equal("54.0", Column(still, "active_p95_ms"));
    }

    [Fact]
    public void WithoutIdleReferenceTheStillColumnsStayEmpty()
    {
        var line = BenchStats.ToCsv(new BenchResult("base", "parado", 1, 2, 3, 60, 0, 0, 1, 1, 1, 10, 0)
        {
            Still = new StillSplit(0, 0, 0, 0, 0, 0) { HasIdleReference = false },
        });

        Assert.Equal("", Column(line, "idle_samples"));
        Assert.Equal("", Column(line, "active_samples"));
    }

    [Fact]
    public void SummarizeUsesTheSameMedianAndP95AsTheRound()
    {
        Assert.Equal((2.5, 4.0, 4.0), BenchStats.Summarize([4, 1, 3, 2]));
        Assert.Equal((0.0, 0.0, 0.0), BenchStats.Summarize([]));
    }
}
