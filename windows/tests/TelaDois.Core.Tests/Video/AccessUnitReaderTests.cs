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

    /// <summary>Entrega no máximo <c>chunk</c> bytes por leitura, como um pipe lento.</summary>
    private sealed class TrickleStream(byte[] data, int chunk) : MemoryStream(data)
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) =>
            base.ReadAsync(buffer[..Math.Min(chunk, buffer.Length)], ct);
    }
}
