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

    /// <summary>Entrega no máximo <c>chunk</c> bytes por leitura, como um pipe lento.</summary>
    private sealed class TrickleStream(byte[] data, int chunk) : MemoryStream(data)
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) =>
            base.ReadAsync(buffer[..Math.Min(chunk, buffer.Length)], ct);
    }
}
