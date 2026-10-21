using TelaDois.LatencyBench;

namespace TelaDois.LatencyBench.Tests;

public class StillSamplesTests
{
    private const int Idle = 60; // tamanho mínimo: o ddagrab só repetindo a imagem

    private static List<Arrival> Frames(int count, Func<int, int>? bytes = null, Func<int, bool>? keyframe = null) =>
        Enumerable.Range(0, count).Select(i => new Arrival(i * 16, keyframe?.Invoke(i) ?? false, bytes?.Invoke(i) ?? Idle)).ToList();

    [Fact]
    public void ChangeAfterOnlyMinimumFramesIsIdleAndAfterBiggerFramesIsActive()
    {
        // imagens 12–15 grandes (o usuário mexendo na tela); trocas em 20 (vê as grandes) e 28 (só mínimas antes)
        var arrivals = Frames(30, i => i is >= 12 and <= 15 ? 2000 : Idle);
        var codes = Enumerable.Range(0, 30).Select(i => (int?)(i < 20 ? 1 : i < 28 ? 2 : 3)).ToList();
        var latencies = Enumerable.Range(0, 30).Select(i => (double?)(i == 20 ? 30 : i == 28 ? 300 : 999)).ToList();

        var split = StillSamples.Split(arrivals, codes, latencies);

        Assert.Equal(1, split.Idle);
        Assert.Equal(300, split.IdleMedianMs);
        Assert.Equal(300, split.IdleP95Ms);
        Assert.Equal(1, split.Active);
        Assert.Equal(30, split.ActiveMedianMs);
    }

    [Fact]
    public void KeyframesInTheLookbackDoNotCountAsActivity()
    {
        // a imagem completa a cada 2 s cai junto das trocas do parado: não pode virar "com atividade"
        var arrivals = Frames(30, i => i == 25 ? 90_000 : Idle, i => i == 25);

        Assert.True(StillSamples.IsIdleBefore(arrivals, 28, Idle));
    }

    [Theory]
    [InlineData(Idle + StillSamples.ToleranceBytes, true)]
    [InlineData(Idle + StillSamples.ToleranceBytes + 1, false)]
    public void ToleranceIsSmall(int bytes, bool idle)
    {
        var arrivals = Frames(30, i => i == 22 ? bytes : Idle);

        Assert.Equal(idle, StillSamples.IsIdleBefore(arrivals, 28, Idle));
    }

    [Fact]
    public void ChangeWithoutAFullLookbackIsActive()
    {
        var arrivals = Frames(30);

        Assert.Equal(10, StillSamples.Lookback);
        Assert.False(StillSamples.IsIdleBefore(arrivals, StillSamples.Lookback - 1, Idle));
        Assert.True(StillSamples.IsIdleBefore(arrivals, StillSamples.Lookback, Idle));
    }

    [Fact]
    public void MinimumSizeIgnoresKeyframesAndUnreadableSamplesAreNotClassified()
    {
        // mínimo do run = 60 (a imagem completa de 40 B não conta); a troca ilegível (null) não entra em nenhum grupo
        var arrivals = Frames(40, i => i == 0 ? 40 : Idle, i => i == 0);
        var codes = Enumerable.Range(0, 40).Select(i => i == 30 ? (int?)null : i < 20 ? 1 : i < 30 ? 2 : 3).ToList();
        var latencies = Enumerable.Range(0, 40).Select(i => i == 30 ? (double?)null : 25).ToList();

        var split = StillSamples.Split(arrivals, codes, latencies);

        Assert.Equal(1, split.Idle); // a troca em 20; a de 31 vem logo depois da ilegível e não é amostra
        Assert.Equal(0, split.Active);
    }

    [Fact]
    public void EmptyInputGivesAnEmptySplit()
    {
        var split = StillSamples.Split([], [], []);

        Assert.Equal(new StillSplit(0, 0, 0, 0, 0, 0), split);
    }
}
