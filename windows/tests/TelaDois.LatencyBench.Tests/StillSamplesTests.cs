using TelaDois.LatencyBench;

namespace TelaDois.LatencyBench.Tests;

public class StillSamplesTests
{
    private const int Idle = 60; // tamanho mínimo: o ddagrab só repetindo a imagem
    private const long Tps = 1000; // 1 tick = 1 ms; uma imagem a cada 16 ms (1 s ≈ 63 imagens)

    private static List<Arrival> Frames(int count, Func<int, int>? bytes = null, Func<int, bool>? keyframe = null) =>
        Enumerable.Range(0, count).Select(i => new Arrival(i * 16, keyframe?.Invoke(i) ?? false, bytes?.Invoke(i) ?? Idle)).ToList();

    // ---- intervalo do parado: 2 s + 0–500 ms, reproduzível pela semente ----

    [Fact]
    public void StillIntervalIsTwoSecondsPlusUpToHalfASecond()
    {
        var next = StillSchedule.Create(seed: 42);
        var intervals = Enumerable.Range(0, 200).Select(_ => next()).ToList();

        Assert.All(intervals, t => Assert.InRange(t, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2.5)));
        Assert.True(intervals.Distinct().Count() > 50); // varia de verdade: a fase em relação à imagem completa muda
    }

    [Fact]
    public void SameSeedGivesTheSameSchedule()
    {
        var a = StillSchedule.Create(7);
        var b = StillSchedule.Create(7);
        var c = StillSchedule.Create(8);

        var first = Enumerable.Range(0, 20).Select(_ => a()).ToList();
        Assert.Equal(first, Enumerable.Range(0, 20).Select(_ => b()).ToList());
        Assert.NotEqual(first, Enumerable.Range(0, 20).Select(_ => c()).ToList());
    }

    // ---- ociosa = tela parada por ≥ 1 s antes da troca ----

    [Fact]
    public void ChangeAfterOneSecondOfMinimumFramesIsIdle()
    {
        var arrivals = Frames(200);

        Assert.True(StillSamples.IsIdleBefore(arrivals, 150, Idle, Tps));
    }

    [Fact]
    public void ABigFrameWithinTheLastSecondIsActivity()
    {
        var arrivals = Frames(200, i => i == 120 ? 6600 : Idle); // 480 ms antes da troca em 150

        Assert.False(StillSamples.IsIdleBefore(arrivals, 150, Idle, Tps));
    }

    [Fact]
    public void ABigFrameMoreThanOneSecondBeforeDoesNotCount()
    {
        var arrivals = Frames(200, i => i == 80 ? 6600 : Idle); // 1,12 s antes da troca em 150 (a troca anterior)

        Assert.True(StillSamples.IsIdleBefore(arrivals, 150, Idle, Tps));
    }

    [Fact]
    public void LessThanOneSecondOfHistoryIsActive()
    {
        var arrivals = Frames(200);

        Assert.Equal(TimeSpan.FromSeconds(1), StillSamples.IdleWindow);
        Assert.False(StillSamples.IsIdleBefore(arrivals, 60, Idle, Tps));  // 960 ms de história
        Assert.True(StillSamples.IsIdleBefore(arrivals, 63, Idle, Tps));   // 1008 ms
    }

    [Theory]
    [InlineData(Idle + StillSamples.ToleranceBytes, true)]
    [InlineData(Idle + StillSamples.ToleranceBytes + 1, false)]
    public void ToleranceIsSmall(int bytes, bool idle)
    {
        var arrivals = Frames(200, i => i == 140 ? bytes : Idle);

        Assert.Equal(idle, StillSamples.IsIdleBefore(arrivals, 150, Idle, Tps));
    }

    // ---- a "cauda" depois da imagem completa é achada pelo tamanho (até 5 imagens) ----

    [Fact]
    public void TheRefinementTailAfterAKeyframeDoesNotCount()
    {
        // medido no h264_qsv (2026-10-21): completa, 126 B, 71 B, depois 60 B
        var sizes = new Dictionary<int, int> { [130] = 203_000, [131] = 126, [132] = 71 };
        var arrivals = Frames(200, i => sizes.GetValueOrDefault(i, Idle), i => i == 130);

        Assert.True(StillSamples.IsIdleBefore(arrivals, 150, Idle, Tps));
    }

    [Fact]
    public void TheTailIsCappedAtFiveFrames()
    {
        Assert.Equal(5, StillSamples.MaxTailFrames);
        var five = Frames(200, i => i == 130 ? 203_000 : i is > 130 and <= 135 ? 500 : Idle, i => i == 130);
        var six = Frames(200, i => i == 130 ? 203_000 : i is > 130 and <= 136 ? 500 : Idle, i => i == 130);

        Assert.True(StillSamples.IsIdleBefore(five, 150, Idle, Tps));
        Assert.False(StillSamples.IsIdleBefore(six, 150, Idle, Tps));
    }

    [Fact]
    public void TheTailEndsAtTheFirstSmallFrame()
    {
        // completa, 60 (fim da cauda), 126: o 126 já é atividade
        var sizes = new Dictionary<int, int> { [130] = 203_000, [132] = 126 };
        var arrivals = Frames(200, i => sizes.GetValueOrDefault(i, Idle), i => i == 130);

        Assert.False(StillSamples.IsIdleBefore(arrivals, 150, Idle, Tps));
    }

    // ---- fase: imagens desde a última completa ----

    [Fact]
    public void FramesSinceKeyframeCountsFromTheLastKeyframeBeforeTheChange()
    {
        var arrivals = Frames(200, keyframe: i => i is 0 or 120);

        Assert.Equal(30, StillSamples.FramesSinceKeyframe(arrivals, 150));
        Assert.Equal(0, StillSamples.FramesSinceKeyframe(arrivals, 120));
        Assert.Null(StillSamples.FramesSinceKeyframe(Frames(10), 5));
    }

    // ---- separação e referência ociosa ----

    private static (List<Arrival> Arrivals, List<int?> Codes, List<double?> Latencies) Run(Func<int, int>? bytes = null)
    {
        // trocas em 70 e 150; atividade (6,6 KB) em 100–110, dentro do último segundo antes da troca em 150
        var arrivals = Frames(200, bytes ?? (i => i is >= 100 and <= 110 ? 6600 : Idle), i => i is 0);
        var codes = Enumerable.Range(0, 200).Select(i => (int?)(i < 70 ? 1 : i < 150 ? 2 : 3)).ToList();
        var latencies = Enumerable.Range(0, 200).Select(i => (double?)(i == 70 ? 300 : i == 150 ? 30 : 999)).ToList();
        return (arrivals, codes, latencies);
    }

    [Fact]
    public void SplitSeparatesIdleFromActiveChanges()
    {
        var (arrivals, codes, latencies) = Run();

        var split = StillSamples.Split(arrivals, codes, latencies, Tps);

        Assert.True(split.HasIdleReference);
        Assert.Equal((1, 300.0, 300.0), (split.Idle, split.IdleMedianMs, split.IdleP95Ms));
        Assert.Equal((1, 30.0), (split.Active, split.ActiveMedianMs));
    }

    [Fact]
    public void ClassifyKeepsTheIndexLatencyAndPhaseOfEachChange()
    {
        var (arrivals, codes, latencies) = Run();

        var samples = StillSamples.Classify(arrivals, codes, latencies, Tps);

        Assert.Equal(
            [new StillSample(70, 300, true, 70), new StillSample(150, 30, false, 150)],
            samples);
    }

    [Fact]
    public void WithoutASmallRepeatedFrameThereIsNoIdleReference()
    {
        // a menor imagem da rodada tem 250 B (> 200 B): ninguém deixou a tela parada, não dá para classificar
        Assert.Equal(200, StillSamples.MaxIdleFrameBytes);
        var (arrivals, codes, latencies) = Run(_ => 250);

        var split = StillSamples.Split(arrivals, codes, latencies, Tps);

        Assert.False(split.HasIdleReference);
        Assert.Equal((0, 0), (split.Idle, split.Active));
        Assert.All(StillSamples.Classify(arrivals, codes, latencies, Tps), s => Assert.Null(s.Idle));
    }

    [Fact]
    public void UnreadableChangesAreNotClassified()
    {
        var (arrivals, codes, latencies) = Run();
        latencies[150] = null;

        Assert.Equal([70], StillSamples.Classify(arrivals, codes, latencies, Tps).Select(s => s.Index));
    }

    [Fact]
    public void PhaseBucketsGroupIdleSamplesByFramesSinceKeyframe()
    {
        StillSample[] samples =
        [
            new(1, 300, true, 5), new(2, 310, true, 20), new(3, 40, true, 45), new(4, 50, true, 50),
            new(5, 999, false, 10), new(6, 999, null, 10),
        ];

        var buckets = StillSamples.ByPhase(samples);

        Assert.Equal(
            [new PhaseBucket("0–29", 2, 305), new PhaseBucket("30–59", 2, 45)],
            buckets);
    }

    [Fact]
    public void EmptyInputHasNoIdleReference()
    {
        var split = StillSamples.Split([], [], [], Tps);

        Assert.False(split.HasIdleReference);
        Assert.Equal((0, 0), (split.Idle, split.Active));
    }
}
