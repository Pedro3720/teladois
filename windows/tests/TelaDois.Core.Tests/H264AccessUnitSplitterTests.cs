using TelaDois.Core.Video;

namespace TelaDois.Core.Tests;

public class H264AccessUnitSplitterTests
{
    private static readonly byte[] Keyframe = Convert.FromHexString("00000001" + "09F0" + "00000001" + "674D001F" + "00000001" + "68EE3C80" + "00000001" + "658884");
    private static readonly byte[] Delta = Convert.FromHexString("000001" + "09F0" + "000001" + "419A02");

    [Fact]
    public void EmitsAccessUnitOnlyWhenTheNextOneStarts()
    {
        var splitter = new H264AccessUnitSplitter();
        Assert.Empty(splitter.Append(Keyframe));
        var units = splitter.Append(Delta);
        Assert.Equal(Keyframe, Assert.Single(units));
    }

    [Fact]
    public void HandlesBytesArrivingOneByOne()
    {
        var splitter = new H264AccessUnitSplitter();
        var units = new List<byte[]>();
        foreach (var b in Keyframe.Concat(Delta).Concat(Keyframe))
        {
            units.AddRange(splitter.Append([b]));
        }
        Assert.Equal(2, units.Count);
        Assert.Equal(Keyframe, units[0]);
        Assert.Equal(Delta, units[1]);
    }

    [Fact]
    public void MergesAudWithoutSliceIntoNextAccessUnit()
    {
        var lonelyAud = Convert.FromHexString("0000000109F0");
        var splitter = new H264AccessUnitSplitter();
        splitter.Append(lonelyAud);
        splitter.Append(Delta);
        var units = splitter.Append(Keyframe);
        Assert.Equal(lonelyAud.Concat(Delta).ToArray(), Assert.Single(units));
    }

    [Fact]
    public void SplitsTheRealFixtureIntoThirtyFrames()
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "testsrc.h264"));
        var splitter = new H264AccessUnitSplitter();
        var units = splitter.Append(bytes).Concat(splitter.Append(Convert.FromHexString("0000000109F0000001419A02"))).ToList();
        Assert.Equal(30, units.Count);
    }
}
