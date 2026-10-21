using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Video;

public class AccessUnitReaderTests
{
    internal static byte[] Fixture() => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "bars-64x64.h264"));

    private static async Task<List<AccessUnit>> ReadAll(Stream stream)
    {
        var units = new List<AccessUnit>();
        await foreach (var unit in new AccessUnitReader(stream).ReadAllAsync())
        {
            units.Add(unit);
        }
        return units;
    }

    [Fact]
    public async Task SplitsFixtureIntoFourFramesWithOnlyFirstKeyframe()
    {
        var units = await ReadAll(new MemoryStream(Fixture()));
        Assert.Equal(4, units.Count);
        Assert.Equal(new[] { true, false, false, false }, units.Select(u => u.IsKeyframe));
        Assert.Equal(Fixture().Length, units.Sum(u => u.Data.Length));
    }

    [Fact]
    public async Task HandlesTinyReadsAndDuplicatedAud()
    {
        var bytes = Convert.FromHexString("0000000109F0").Concat(Fixture()).ToArray();
        var units = await ReadAll(new TrickleStream(bytes, 3));
        Assert.Equal(4, units.Count);
        Assert.True(units[0].IsKeyframe);
    }

    [Fact]
    public void ScanFindsThreeAndFourByteStartCodes()
    {
        var data = Convert.FromHexString("0000000109F000000165AABB");
        Assert.Equal(new[] { (0, 9), (6, 5) }, H264.ScanNals(data, data.Length));
    }

    private static readonly byte[] Aud = Convert.FromHexString("0000000109F0");
    private static readonly byte[] Sps = Convert.FromHexString("000000016742001E");
    private static readonly byte[] Pps = Convert.FromHexString("0000000168CE3880");
    private static readonly byte[] Idr = Convert.FromHexString("0000000165888480");
    private static readonly byte[] Slice = Convert.FromHexString("00000001419A80");

    private static byte[] Concat(params byte[][] parts) => parts.SelectMany(p => p).ToArray();

    [Fact]
    public async Task ReinsertsLastParameterSetsBeforeAnIdrThatCameWithout()
    {
        // Como o h264_amf: SPS/PPS só na primeira imagem completa (medido no PC AMD em 2026-10-19).
        var units = await ReadAll(new MemoryStream(Concat(Aud, Sps, Pps, Idr, Aud, Slice, Aud, Idr)));
        Assert.Equal(3, units.Count);
        Assert.Equal(Concat(Aud, Sps, Pps, Idr), units[0].Data);
        Assert.Equal(Concat(Aud, Slice), units[1].Data);
        Assert.Equal(Concat(Aud, Sps, Pps, Idr), units[2].Data);
        Assert.True(units[2].IsKeyframe);
    }

    [Fact]
    public async Task OnlyAddsTheMissingParameterSet()
    {
        // Veio um SPS novo sem PPS: só o PPS guardado entra, logo depois do AUD (antes do SPS que veio).
        var newSps = Convert.FromHexString("000000016742001F");
        var units = await ReadAll(new MemoryStream(Concat(Aud, Sps, Pps, Idr, Aud, newSps, Idr)));
        Assert.Equal(Concat(Aud, Pps, newSps, Idr), units[1].Data);
    }

    [Fact]
    public async Task LeavesAnIdrAloneWhenNoParameterSetWasSeenYet()
    {
        var units = await ReadAll(new MemoryStream(Concat(Aud, Idr, Aud, Slice)));
        Assert.Equal(Concat(Aud, Idr), units[0].Data);
    }

    // ---- Fluxo emoldurado: "TD <tamanho>\n" + pacote (o -stats_mux_pre do FFmpeg escreve a linha logo antes de cada pacote).

    private static byte[] TestsrcRaw() => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "testsrc2-64x64.h264"));

    private static byte[] TestsrcFramed() => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "testsrc2-64x64.framed"));

    private static byte[] Framed(params byte[][] packets) =>
        packets.SelectMany(p => System.Text.Encoding.ASCII.GetBytes($"TD {p.Length}\n").Concat(p)).ToArray();

    [Fact]
    public async Task FramedFixtureGivesExactlyTheUnitsOfTheRawStream()
    {
        var framed = await ReadAll(new MemoryStream(TestsrcFramed()));
        var raw = await ReadAll(new MemoryStream(TestsrcRaw()));

        Assert.Equal(5, framed.Count);
        Assert.Equal(new[] { true, false, false, true, false }, framed.Select(u => u.IsKeyframe));
        Assert.Equal(raw.Select(u => u.IsKeyframe), framed.Select(u => u.IsKeyframe));
        for (var i = 0; i < raw.Count; i++)
        {
            Assert.Equal(raw[i].Data, framed[i].Data);
        }
        Assert.Equal(TestsrcRaw(), framed.SelectMany(u => u.Data).ToArray());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(7)]
    [InlineData(4096)]
    public async Task FramedSurvivesAnyReadSize(int chunk)
    {
        var units = await ReadAll(new TrickleStream(TestsrcFramed(), chunk));
        Assert.Equal(TestsrcRaw(), units.SelectMany(u => u.Data).ToArray());
        Assert.Equal(5, units.Count);
    }

    [Fact]
    public async Task FramedSurvivesRandomReadSizes()
    {
        var random = new Random(5);
        var units = await ReadAll(new RandomChunkStream(TestsrcFramed(), random, 300));
        Assert.Equal(TestsrcRaw(), units.SelectMany(u => u.Data).ToArray());
    }

    [Fact]
    public async Task FramedUnitIsYieldedAsSoonAsItsLastByteArrives()
    {
        var unit = Concat(Aud, Sps, Pps, Idr);
        var bytes = Framed(unit, Concat(Aud, Slice));
        var firstEnd = bytes.Length - Framed(Concat(Aud, Slice)).Length;
        var feed = new FeedStream();
        await using var e = new AccessUnitReader(feed).ReadAllAsync().GetAsyncEnumerator();

        var next = e.MoveNextAsync().AsTask();
        feed.Write(bytes[..(firstEnd - 1)]);
        await Task.Delay(100);
        Assert.False(next.IsCompleted); // falta 1 byte

        feed.Write(bytes[(firstEnd - 1)..firstEnd]);
        Assert.True(await next.WaitAsync(TimeSpan.FromSeconds(5))); // sem esperar nada da imagem seguinte
        Assert.Equal(unit, e.Current.Data);
        Assert.True(e.Current.IsKeyframe);

        feed.Write(bytes[firstEnd..]);
        feed.Complete();
        Assert.True(await e.MoveNextAsync());
        Assert.Equal(Concat(Aud, Slice), e.Current.Data);
        Assert.False(await e.MoveNextAsync());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4093)]
    [InlineData(65536)]
    public async Task FramedIdrLargerThanThePipeBufferStaysWhole(int chunk)
    {
        var big = new byte[300_000];
        Array.Fill(big, (byte)0xAB);
        var idr = Concat(Aud, Sps, Pps, Convert.FromHexString("0000000165"), big);
        var units = await ReadAll(new TrickleStream(Framed(idr, Concat(Aud, Slice)), chunk));
        Assert.Equal(2, units.Count);
        Assert.Equal(idr, units[0].Data);
        Assert.True(units[0].IsKeyframe);
    }

    [Fact]
    public async Task FramedMultiSliceFrameIsOneUnit()
    {
        var twoSlices = Concat(Aud, Sps, Pps, Idr, Convert.FromHexString("0000016588AA"));
        var units = await ReadAll(new MemoryStream(Framed(twoSlices, Concat(Aud, Slice, Convert.FromHexString("000001419BCC")))));
        Assert.Equal(2, units.Count);
        Assert.Equal(twoSlices, units[0].Data);
    }

    [Fact]
    public async Task FramedReinsertsParameterSetsBeforeAnIdrThatCameWithout()
    {
        var units = await ReadAll(new MemoryStream(Framed(Concat(Aud, Sps, Pps, Idr), Concat(Aud, Slice), Concat(Aud, Idr))));
        Assert.Equal(3, units.Count);
        Assert.Equal(Concat(Aud, Slice), units[1].Data);
        Assert.Equal(Concat(Aud, Sps, Pps, Idr), units[2].Data);
        Assert.True(units[2].IsKeyframe);
    }

    [Fact]
    public async Task FramedPacketWithoutSliceIsDropped()
    {
        var units = await ReadAll(new MemoryStream(Framed(Concat(Aud, Sps, Pps), Concat(Aud, Sps, Pps, Idr))));
        Assert.Equal(Concat(Aud, Sps, Pps, Idr), Assert.Single(units).Data);
    }

    [Fact]
    public async Task FramedTruncatedPacketAtEndOfStreamIsDropped()
    {
        var whole = Framed(Concat(Aud, Sps, Pps, Idr));
        var cut = Framed(Concat(Aud, Slice))[..^2];
        var units = await ReadAll(new MemoryStream(Concat(whole, cut)));
        Assert.Equal(Concat(Aud, Sps, Pps, Idr), Assert.Single(units).Data);
    }

    [Fact]
    public async Task FramedTruncatedHeaderAtEndOfStreamIsDropped()
    {
        var units = await ReadAll(new MemoryStream(Concat(Framed(Concat(Aud, Slice)), System.Text.Encoding.ASCII.GetBytes("TD 12"))));
        Assert.Single(units);
    }

    [Theory]
    [InlineData("TD x\n")]
    [InlineData("TD \n")]
    [InlineData("TD -5\n")]
    [InlineData("TX 5\n")]
    [InlineData("TD 99999999999\n")]
    public async Task FramedBadHeaderFailsTheRead(string header)
    {
        var bytes = Concat(Framed(Concat(Aud, Slice)), System.Text.Encoding.ASCII.GetBytes(header), Concat(Aud, Slice));
        await Assert.ThrowsAsync<InvalidDataException>(() => ReadAll(new MemoryStream(bytes)));
    }

    [Fact]
    public async Task FramedHeaderWithoutNewlineFailsTheRead()
    {
        var bytes = Concat(System.Text.Encoding.ASCII.GetBytes("TD 1" + new string('0', 40)), Aud);
        await Assert.ThrowsAsync<InvalidDataException>(() => ReadAll(new MemoryStream(bytes)));
    }

    [Fact]
    public async Task FramedSizeThatDoesNotMatchThePacketFailsTheRead()
    {
        // Tamanho 2 bytes menor: o pacote seguinte não começa por "TD ".
        var packet = Concat(Aud, Slice);
        var bytes = Concat(System.Text.Encoding.ASCII.GetBytes($"TD {packet.Length - 2}\n"), packet, Framed(Concat(Aud, Slice)));
        await Assert.ThrowsAsync<InvalidDataException>(() => ReadAll(new MemoryStream(bytes)));
    }

    [Fact]
    public async Task FramedPacketThatIsNotAnnexBFailsTheRead()
    {
        var bytes = Framed([0x12, 0x34, 0x56, 0x78, 0x9A]);
        await Assert.ThrowsAsync<InvalidDataException>(() => ReadAll(new MemoryStream(bytes)));
    }

    /// <summary>
    /// Com o FFmpeg real (só onde ele está instalado): a linha do app (emoldurada) e a mesma linha sem a moldura dão as
    /// mesmas imagens, byte a byte. IDR de 1460×674 passa de 64 KB e é lida em vários pedaços do pipe.
    /// </summary>
    [Theory]
    [InlineData("x264")]
    [InlineData("qsv-download")]
    public async Task RealFfmpegFramedLineGivesTheSameUnitsAsTheRawLine(string id)
    {
        var ffmpeg = FfmpegLocator.Find();
        if (ffmpeg is null)
        {
            return; // FFmpeg ausente (ex.: CI): o teste só roda onde ele está instalado.
        }
        var catalog = EncoderProfiles.Candidates.Single(p => p.Id == id);
        // Mesma compressão e opções; a entrada é sintética (sem placa: o filtro do perfil começa por hwdownload).
        var profile = catalog with { Filter = id == "x264" ? "format=yuv420p" : "format=nv12" };
        // -threads 1: com várias threads o libx264 não repete os mesmos bytes de uma execução para outra (medido).
        var settings = new EncodeSettings { Bitrate = "40M", Maxrate = "40M", ExtraCodecOptions = id == "x264" ? ["-threads", "1"] : [] };
        var framed = FfmpegArguments.Encode(profile, "testsrc2=size=1460x674:rate=60", 60, frames: 30, settings: settings);
        var stats = framed.ToList().IndexOf("-flush_packets");
        var raw = framed.Take(stats).Concat(framed.Skip(stats + 6)).ToList();
        Assert.Equal("-f", raw[^3]);

        var fromFramed = await RunAsync(ffmpeg, framed);
        var fromRaw = await RunAsync(ffmpeg, raw);
        if (fromRaw.Count == 0 && id != "x264")
        {
            return; // sem Intel QSV nesta máquina
        }
        Assert.Equal(30, fromRaw.Count);
        Assert.Equal(fromRaw.Count, fromFramed.Count);
        for (var i = 0; i < fromRaw.Count; i++)
        {
            Assert.Equal(fromRaw[i].IsKeyframe, fromFramed[i].IsKeyframe);
            Assert.Equal(fromRaw[i].Data, fromFramed[i].Data);
        }
        Assert.True(fromFramed[0].Data.Length > 64 * 1024, $"IDR de {fromFramed[0].Data.Length} bytes");

        static async Task<List<AccessUnit>> RunAsync(string ffmpeg, IReadOnlyList<string> args)
        {
            await using var process = new SystemProcessRunner().Start(ffmpeg, args);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var units = new List<AccessUnit>();
            await foreach (var unit in new AccessUnitReader(process.Output).ReadAllAsync(cts.Token))
            {
                units.Add(unit);
            }
            return units;
        }
    }

    /// <summary>Leituras de tamanho aleatório entre 1 e <c>max</c> bytes.</summary>
    private sealed class RandomChunkStream(byte[] data, Random random, int max) : MemoryStream(data)
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) =>
            base.ReadAsync(buffer[..Math.Min(random.Next(1, max + 1), buffer.Length)], ct);
    }

    /// <summary>Um pipe controlado pelo teste: cada leitura espera o próximo pedaço escrito.</summary>
    private sealed class FeedStream : Stream
    {
        private readonly System.Threading.Channels.Channel<byte[]> _chunks = System.Threading.Channels.Channel.CreateUnbounded<byte[]>();
        private byte[] _current = [];
        private int _at;

        public void Write(byte[] chunk) => _chunks.Writer.TryWrite(chunk);

        public void Complete() => _chunks.Writer.TryComplete();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            while (_at == _current.Length)
            {
                if (!await _chunks.Reader.WaitToReadAsync(ct))
                {
                    return 0;
                }
                _chunks.Reader.TryRead(out var next);
                (_current, _at) = (next!, 0);
            }
            var n = Math.Min(buffer.Length, _current.Length - _at);
            _current.AsMemory(_at, n).CopyTo(buffer);
            _at += n;
            return n;
        }

        public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    /// <summary>Entrega no máximo <c>chunk</c> bytes por leitura, como um pipe lento.</summary>
    private sealed class TrickleStream(byte[] data, int chunk) : MemoryStream(data)
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) =>
            base.ReadAsync(buffer[..Math.Min(chunk, buffer.Length)], ct);
    }
}
