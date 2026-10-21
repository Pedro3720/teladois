using TelaDois.Core.Video;
using TelaDois.LatencyBench;

namespace TelaDois.LatencyBench.Tests;

public class BenchVariantsTests
{
    private static BenchVariant Get(string name) => BenchVariants.All.Single(v => v.Name == name);

    [Fact]
    public void NamesAreUniqueAndInTheAgreedOrder()
    {
        string[] expected =
        [
            "base", "lowpower", "veryfast", "cbr", "intrarefresh", "gop4", "nodup", "download", "scale1920", "scale2of3",
            "veryfast-scale1920", "veryfast-scale2of3", "x264", "mf", "fps30",
        ];
        Assert.Equal(expected, BenchVariants.All.Select(v => v.Name).ToArray());
        Assert.Equal(BenchVariants.All.Count, BenchVariants.All.Select(v => v.Name).Distinct().Count());
    }

    [Fact]
    public void BaseIsTodaysDefault()
    {
        var b = Get("base");
        Assert.Equal("qsv-hwmap", b.ProfileId);
        Assert.Equal(EncodeSettings.Default, b.Settings);
    }

    [Fact]
    public void DownloadUsesTheDownloadProfileWithDefaults()
    {
        var d = Get("download");
        Assert.Equal("qsv-download", d.ProfileId);
        Assert.Equal(EncodeSettings.Default, d.Settings);
    }

    [Fact]
    public void EveryProfileIdExistsInTheCatalog()
    {
        var ids = EncoderProfiles.Candidates.Select(p => p.Id).ToHashSet();
        foreach (var v in BenchVariants.All)
        {
            Assert.Contains(v.ProfileId, ids);
        }
    }

    [Fact]
    public void ScaledVariantsHaveEvenSizes()
    {
        Assert.Equal((1920, 886), Get("scale1920").Settings.ScaleTo);
        var s = Get("scale2of3").Settings.ScaleTo!.Value;
        Assert.Equal((1704, 786), s);
        Assert.Equal(0, s.Width % 2);
        Assert.Equal(0, s.Height % 2);
    }

    [Fact]
    public void OtherVariantsChangeOnlyWhatTheirNameSays()
    {
        Assert.Equal("1", Get("lowpower").Settings.CodecOptionOverrides["-low_power"]);
        Assert.Equal("veryfast", Get("veryfast").Settings.CodecOptionOverrides["-preset"]);

        var cbr = Get("cbr").Settings;
        Assert.Equal("12M", cbr.Bitrate);
        Assert.Equal("12M", cbr.Maxrate);
        Assert.Equal("200k", cbr.Bufsize);

        var ir = Get("intrarefresh").Settings;
        Assert.Equal(["-int_ref_type", "vertical", "-int_ref_cycle_size", "60"], ir.ExtraCodecOptions);
        Assert.Equal(10, ir.GopSeconds);

        Assert.Equal(4, Get("gop4").Settings.GopSeconds);
        Assert.Equal(":dup_frames=0", Get("nodup").Settings.GrabOptions);
    }

    [Theory]
    [InlineData("veryfast-scale1920", 1920, 886)]
    [InlineData("veryfast-scale2of3", 1704, 786)]
    public void CombinationsJoinVeryfastAndTheScale(string name, int width, int height)
    {
        var v = Get(name);
        Assert.Equal("qsv-hwmap", v.ProfileId);
        Assert.Equal((width, height), v.Settings.ScaleTo);
        Assert.Equal("veryfast", Assert.Single(v.Settings.CodecOptionOverrides).Value);
        Assert.Equal("-preset", Assert.Single(v.Settings.CodecOptionOverrides).Key);
        // O resto igual à base: só preset e tamanho mudam.
        Assert.Equal(EncodeSettings.Default with { CodecOptionOverrides = v.Settings.CodecOptionOverrides, ScaleTo = (width, height) }, v.Settings);
        Assert.Equal(BenchVariant.DefaultFps, v.Fps);
    }

    [Theory]
    [InlineData("x264")]
    [InlineData("mf")]
    public void SoftwareAndMediaFoundationDiagnosticsUseTheirProfileWithDefaults(string name)
    {
        var v = Get(name);
        Assert.Equal(name, v.ProfileId);
        Assert.Equal(EncodeSettings.Default, v.Settings);
        Assert.Equal(BenchVariant.DefaultFps, v.Fps);
    }

    [Fact]
    public void Fps30CapturesAndEncodesAt30WithTheSameGopInSeconds()
    {
        var v = Get("fps30");
        Assert.Equal("qsv-hwmap", v.ProfileId);
        Assert.Equal(EncodeSettings.Default, v.Settings);
        Assert.Equal(30, v.Fps);

        var profile = EncoderProfiles.Candidates.Single(p => p.Id == v.ProfileId);
        var args = FfmpegArguments.ForMonitor(profile, 2, v.Fps, v.Settings);
        Assert.Equal("ddagrab=output_idx=2:framerate=30:draw_mouse=1", args[args.ToList().IndexOf("-i") + 1]);
        Assert.Equal("60", args[args.ToList().IndexOf("-g") + 1]);
    }

    [Fact]
    public void OnlyFps30ChangesTheFrameRate()
    {
        Assert.Equal(60, BenchVariant.DefaultFps);
        Assert.All(BenchVariants.All.Where(v => v.Name != "fps30"), v => Assert.Equal(BenchVariant.DefaultFps, v.Fps));
    }
}
