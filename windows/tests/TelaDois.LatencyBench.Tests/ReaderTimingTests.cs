using TelaDois.Core.Video;
using TelaDois.LatencyBench;

namespace TelaDois.LatencyBench.Tests;

public class ReaderTimingTests
{
    private static readonly byte[] Aud = [0, 0, 0, 1, 0x09, 0xF0];
    private static readonly byte[] Sps = [0, 0, 0, 1, 0x67, 0x42, 0x00, 0x1F];
    private static readonly byte[] Pps = [0, 0, 0, 1, 0x68, 0xCE, 0x38];
    private static readonly byte[] Idr = [0, 0, 0, 1, 0x65, 0x88, 0x84, 0x21, 0x43];
    private static readonly byte[] Slice = [0, 0, 0, 1, 0x41, 0x9A, 0x22, 0x11];

    // AUD+SPS+PPS+IDR · AUD+fatia · AUD+SPS (sem fatia: o leitor descarta) · AUD+fatia
    private static byte[] Stream()
    {
        byte[][] parts = [Aud, Sps, Pps, Idr, Aud, Slice, Aud, Sps, Aud, Slice];
        return parts.SelectMany(p => p).ToArray();
    }

    /// <summary>Devolve no máximo <c>chunk</c> bytes por leitura (o pipe do FFmpeg entrega aos pedaços).</summary>
    private sealed class ChunkedStream(byte[] data, int chunk) : MemoryStream(data)
    {
        public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(count, chunk));

        public override int Read(Span<byte> buffer) => base.Read(buffer[..Math.Min(buffer.Length, chunk)]);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) =>
            base.ReadAsync(buffer[..Math.Min(buffer.Length, chunk)], ct);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) =>
            base.ReadAsync(buffer, offset, Math.Min(count, chunk), ct);
    }

    [Fact]
    public void UnitEndsFollowTheReaderRule()
    {
        var data = Stream();
        var first = Aud.Length + Sps.Length + Pps.Length + Idr.Length;
        var second = first + Aud.Length + Slice.Length;
        var third = second + Aud.Length + Sps.Length; // segmento sem fatia: não é imagem

        Assert.Equal([first, second, data.Length], ReaderTiming.UnitEnds(data));
        Assert.True(third < data.Length);
    }

    [Fact]
    public void UnitEndsMatchTheRealReaderCount()
    {
        var data = Stream();

        var units = new AccessUnitReader(new MemoryStream(data)).ReadAllAsync().ToBlockingEnumerable().ToList();

        Assert.Equal(units.Count, ReaderTiming.UnitEnds(data).Count);
    }

    [Fact]
    public void LastByteTicksIsTheFirstReadThatReachedTheEnd()
    {
        ReadStamp[] reads = [new(10, 100), new(25, 200), new(40, 300)];

        Assert.Equal(new long?[] { 100, 200, 200, 300, null }, ReaderTiming.LastByteTicks(reads, [5, 11, 25, 26, 41]));
    }

    [Fact]
    public void StampsEveryReadWithTheCumulativeEndAndKeepsTheBytes()
    {
        var data = Stream();
        var clock = 0L;
        var stamped = new TimestampedStream(new ChunkedStream(data, 7), () => ++clock);

        var buffer = new byte[1000];
        var read = 0;
        int n;
        while ((n = stamped.Read(buffer, read, buffer.Length - read)) > 0)
        {
            read += n;
        }

        Assert.Equal(data, stamped.RawBytes());
        Assert.Equal(Enumerable.Range(1, (data.Length + 6) / 7).Select(i => (long)Math.Min(i * 7, data.Length)), stamped.Reads.Select(r => r.EndOffset));
        Assert.Equal(Enumerable.Range(1, stamped.Reads.Count).Select(i => (long)i), stamped.Reads.Select(r => r.Ticks));
    }

    [Fact]
    public async Task IdealTimeIsNeverLaterThanWhenTheReaderYields()
    {
        // o leitor só entrega a imagem quando chega o AUD da próxima; o "leitor ideal" a teria no último byte dela
        var data = Stream();
        var clock = 0L;
        var stamped = new TimestampedStream(new ChunkedStream(data, 5), () => ++clock);
        var yielded = new List<long>();
        await foreach (var _ in new AccessUnitReader(stamped).ReadAllAsync())
        {
            yielded.Add(clock);
        }

        var ideal = ReaderTiming.LastByteTicks(stamped.Reads, ReaderTiming.UnitEnds(stamped.RawBytes()));

        Assert.Equal(yielded.Count, ideal.Count);
        for (var i = 0; i < ideal.Count; i++)
        {
            Assert.NotNull(ideal[i]);
            Assert.True(ideal[i] <= yielded[i], $"imagem {i}: ideal {ideal[i]} > entregue {yielded[i]}");
        }
        Assert.True(ideal[0] < yielded[0]); // a 1ª imagem esperou o AUD da 2ª
    }

    // ---- Fluxo emoldurado (o do app desde a Task 5 da 5d): "TD <tamanho>\n" + pacote.

    private static byte[] Framed(params byte[][] packets) =>
        packets.SelectMany(p => System.Text.Encoding.ASCII.GetBytes($"TD {p.Length}\n").Concat(p)).ToArray();

    private static byte[] Cat(params byte[][] parts) => parts.SelectMany(p => p).ToArray();

    [Fact]
    public void UnitEndsOfAFramedStreamAreThePacketEnds()
    {
        byte[][] packets = [Cat(Aud, Sps, Pps, Idr), Cat(Aud, Slice), Cat(Aud, Sps), Cat(Aud, Slice)];
        var data = Cat(Framed(packets), Framed(Cat(Aud, Slice))[..^1]); // a última cortada pelo fim: o leitor descarta
        var first = Framed(packets[0]).Length;
        var second = first + Framed(packets[1]).Length;
        var fourth = second + Framed(packets[2]).Length + Framed(packets[3]).Length; // a 3ª não tem fatia

        Assert.Equal([first, second, fourth], ReaderTiming.UnitEnds(data));
        Assert.Equal(3, new AccessUnitReader(new MemoryStream(data)).ReadAllAsync().ToBlockingEnumerable().Count());
    }

    [Fact]
    public async Task OnAFramedStreamTheReaderYieldsAtTheIdealTime()
    {
        var data = Framed(Cat(Aud, Sps, Pps, Idr), Cat(Aud, Slice), Cat(Aud, Sps), Cat(Aud, Slice));
        var clock = 0L;
        var stamped = new TimestampedStream(new ChunkedStream(data, 5), () => ++clock);
        var yielded = new List<long>();
        await foreach (var _ in new AccessUnitReader(stamped).ReadAllAsync())
        {
            yielded.Add(clock);
        }

        var ideal = ReaderTiming.LastByteTicks(stamped.Reads, ReaderTiming.UnitEnds(stamped.RawBytes()));

        Assert.Equal(yielded.Select(t => (long?)t), ideal);
    }
}
