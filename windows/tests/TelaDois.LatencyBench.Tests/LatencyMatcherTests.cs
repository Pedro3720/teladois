using TelaDois.LatencyBench;

namespace TelaDois.LatencyBench.Tests;

public class LatencyMatcherTests
{
    private const long Tps = 1000; // 1 tick = 1 ms

    [Fact]
    public void MatchesEachArrivalToTheDrawWithTheSameCode()
    {
        var draws = new[] { new DrawEvent(1, 0), new DrawEvent(2, 16), new DrawEvent(3, 33) };
        var arrivals = new[] { new Arrival(40, true, 100), new Arrival(60, false, 10), new Arrival(70, false, 10) };
        var codes = new int?[] { 2, 3, null };

        var result = LatencyMatcher.Match(draws, arrivals, codes, Tps);

        Assert.Equal(3, result.Count);
        Assert.Equal(24, result[0]!.Value, 6);
        Assert.Equal(27, result[1]!.Value, 6);
        Assert.Null(result[2]);
    }

    [Fact]
    public void RepeatedCodeUsesTheMostRecentDrawBeforeTheArrival()
    {
        var draws = new[] { new DrawEvent(7, 0), new DrawEvent(8, 10), new DrawEvent(7, 30), new DrawEvent(7, 90) };
        var arrivals = new[] { new Arrival(50, false, 1) };

        var result = LatencyMatcher.Match(draws, arrivals, new int?[] { 7 }, Tps);

        Assert.Equal(20, result[0]!.Value, 6); // o desenho de 30 ms, não o de 0 nem o de 90 (depois da chegada)
    }

    [Fact]
    public void CodeWithoutADrawIsNull()
    {
        var draws = new[] { new DrawEvent(1, 0) };
        var arrivals = new[] { new Arrival(10, false, 1), new Arrival(20, false, 1) };

        var result = LatencyMatcher.Match(draws, arrivals, new int?[] { 99, 1 }, Tps);

        Assert.Null(result[0]);
        Assert.Equal(20, result[1]!.Value, 6);
    }

    [Fact]
    public void DrawAfterTheArrivalDoesNotCount()
    {
        var draws = new[] { new DrawEvent(5, 100) };
        var arrivals = new[] { new Arrival(50, false, 1) };

        Assert.Null(LatencyMatcher.Match(draws, arrivals, new int?[] { 5 }, Tps)[0]);
    }

    [Fact]
    public void ConvertsTicksToMilliseconds()
    {
        var draws = new[] { new DrawEvent(1, 0) };
        var arrivals = new[] { new Arrival(50_000, false, 1) };

        // 10 MHz: 50 000 ticks = 5 ms
        Assert.Equal(5, LatencyMatcher.Match(draws, arrivals, new int?[] { 1 }, 10_000_000)[0]!.Value, 6);
    }

    [Fact]
    public void EmptyInputsGiveEmptyOrNull()
    {
        Assert.Empty(LatencyMatcher.Match([], [], [], Tps));
        Assert.Null(LatencyMatcher.Match([], [new Arrival(1, false, 1)], new int?[] { 1 }, Tps)[0]);
    }

    [Fact]
    public void FirstChangesOnlyKeepsTheFrameWhereANewCodeShowsUp()
    {
        // o ddagrab repete imagens com a tela parada: só a 1ª de cada código novo é amostra de atraso
        var codes = new int?[] { 1, 1, 1, 2, 2, 3 };
        var latencies = new double?[] { 900, 920, 940, 30, 46, 28 };

        Assert.Equal(new double?[] { 30, 28 }, LatencyMatcher.FirstChangesOnly(codes, latencies));
    }

    [Fact]
    public void FirstChangesOnlyDropsTheFirstFrameBecauseItsChangeWasNotSeen()
    {
        var codes = new int?[] { 5, 6, 7 };
        var latencies = new double?[] { 1000, 20, 21 };

        Assert.Equal(new double?[] { 20, 21 }, LatencyMatcher.FirstChangesOnly(codes, latencies));
    }

    [Fact]
    public void FirstChangesOnlyKeepsUnreadableFramesAsMissingAndComparesWithTheLastReadCode()
    {
        // null antes do 1º código lido não conta; null depois conta (falta); 4 depois do null ainda é repetição
        var codes = new int?[] { null, 4, null, 4, 5 };
        var latencies = new double?[] { null, 500, null, 520, 25 };

        Assert.Equal(new double?[] { null, 25 }, LatencyMatcher.FirstChangesOnly(codes, latencies));
    }

    [Fact]
    public void FirstChangesOnlyUsesTheShorterOfTheTwoLists()
    {
        var codes = new int?[] { 1, 2, 3, 4 };
        var latencies = new double?[] { 10, 20 };

        Assert.Equal(new double?[] { 20 }, LatencyMatcher.FirstChangesOnly(codes, latencies));
        Assert.Empty(LatencyMatcher.FirstChangesOnly([], []));
    }
}
