using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Video;

public class FrameQueueTests
{
    private long _nowMs; // relógio falso: os testes avançam o tempo à mão

    private static AccessUnit Frame(byte id, bool key = false) => new([id], key);

    private FrameQueue NewQueue(List<long>? episodes = null)
    {
        var queue = new FrameQueue(TimeSpan.FromMilliseconds(100), () => _nowMs);
        if (episodes is not null)
        {
            queue.DropStarted += () => episodes.Add(queue.Dropped);
        }
        return queue;
    }

    private static async Task<byte[]> TakeIds(FrameQueue queue, int count)
    {
        var ids = new byte[count];
        for (var i = 0; i < count; i++)
        {
            ids[i] = (await queue.TakeAsync(CancellationToken.None)).Data[0];
        }
        return ids;
    }

    [Fact]
    public async Task DeliversInOrderWithTheDefaultClock()
    {
        var queue = new FrameQueue();
        queue.Add(Frame(1, true));
        queue.Add(Frame(2));
        Assert.Equal(new byte[] { 1, 2 }, await TakeIds(queue, 2));
    }

    [Fact]
    public async Task BurstWithinMaxAgeIsDeliveredWhole()
    {
        // Engasgo do PC: várias imagens chegam de uma leitura só. Nada passou de 100 ms esperando: nada é descartado.
        var episodes = new List<long>();
        var queue = NewQueue(episodes);
        queue.Add(Frame(1, true));
        for (byte id = 2; id <= 10; id++)
        {
            queue.Add(Frame(id));
        }
        _nowMs = 100; // a mais antiga tem exatamente 100 ms: ainda vale
        queue.Add(Frame(11));
        Assert.Equal(Enumerable.Range(1, 11).Select(i => (byte)i), await TakeIds(queue, 11));
        Assert.Equal(0, queue.Dropped);
        Assert.Empty(episodes);
    }

    [Fact]
    public async Task WhenOldestWaitingIsTooOldDropsUntilNextKeyframe()
    {
        var episodes = new List<long>();
        var queue = NewQueue(episodes);
        queue.Add(Frame(1, true));
        queue.Add(Frame(2));
        _nowMs = 101;
        queue.Add(Frame(3));        // a 1 esperou 101 ms: descarta 1, 2 e 3
        queue.Add(Frame(4));        // ainda esperando imagem completa: descarta
        queue.Add(Frame(5, true));  // completa: volta a entregar
        queue.Add(Frame(6));
        Assert.Equal(new byte[] { 5, 6 }, await TakeIds(queue, 2));
        Assert.Equal(4, queue.Dropped);
        Assert.Equal(new long[] { 3 }, episodes); // um aviso por episódio, com o total até ali
    }

    [Fact]
    public async Task KeyframeArrivingLateReplacesTheBacklog()
    {
        var episodes = new List<long>();
        var queue = NewQueue(episodes);
        queue.Add(Frame(1, true));
        queue.Add(Frame(2));
        _nowMs = 150;
        queue.Add(Frame(3, true));
        queue.Add(Frame(4));        // não ficou esperando completa: entra
        Assert.Equal(new byte[] { 3, 4 }, await TakeIds(queue, 2));
        Assert.Equal(2, queue.Dropped);
        Assert.Equal(new long[] { 2 }, episodes);
    }

    [Fact]
    public async Task AgeCountsFromTheOldestStillWaiting()
    {
        var queue = NewQueue();
        queue.Add(Frame(1, true));
        _nowMs = 60;
        queue.Add(Frame(2));
        Assert.Equal(new byte[] { 1 }, await TakeIds(queue, 1));
        _nowMs = 150; // a 1 teria 150 ms, mas já saiu; a 2 tem 90 ms
        queue.Add(Frame(3));
        Assert.Equal(new byte[] { 2, 3 }, await TakeIds(queue, 2));
        Assert.Equal(0, queue.Dropped);
    }

    [Fact]
    public async Task EachEpisodeIsReportedOnce()
    {
        var episodes = new List<long>();
        var queue = NewQueue(episodes);
        queue.Add(Frame(1, true));
        _nowMs = 200;
        queue.Add(Frame(2));        // episódio 1: descarta 1 e 2
        queue.Add(Frame(3));
        queue.Add(Frame(4, true));
        _nowMs = 400;
        queue.Add(Frame(5));        // episódio 2: descarta 4 e 5
        queue.Add(Frame(6, true));
        Assert.Equal(new byte[] { 6 }, await TakeIds(queue, 1));
        Assert.Equal(5, queue.Dropped);
        Assert.Equal(new long[] { 2, 5 }, episodes);
    }

    [Fact]
    public async Task SafetyCapDropsWhen240AreWaiting()
    {
        var episodes = new List<long>();
        var queue = NewQueue(episodes);
        queue.Add(Frame(1, true));
        for (var i = 1; i < FrameQueue.MaxPending; i++)
        {
            queue.Add(Frame(2));
        }
        Assert.Equal(0, queue.Dropped);
        queue.Add(Frame(3));        // 240 esperando, todas recentes: o teto de memória descarta assim mesmo
        queue.Add(Frame(4, true));
        Assert.Equal(new byte[] { 4 }, await TakeIds(queue, 1));
        Assert.Equal(FrameQueue.MaxPending + 1, queue.Dropped);
        Assert.Single(episodes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RejectsMaxAgeThatIsNotPositive(int milliseconds)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FrameQueue(TimeSpan.FromMilliseconds(milliseconds)));
    }

    [Fact]
    public async Task TakeWaitsAndHonorsCancellation()
    {
        var queue = NewQueue();
        using var cts = new CancellationTokenSource();
        var take = queue.TakeAsync(cts.Token);
        Assert.False(take.IsCompleted);
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => take);
    }
}
