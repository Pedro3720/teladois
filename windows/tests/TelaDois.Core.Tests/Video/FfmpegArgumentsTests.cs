using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Video;

public class FfmpegArgumentsTests
{
    private static readonly EncoderProfile Qsv = EncoderProfiles.Candidates.Single(p => p.Id == "qsv-hwmap");

    [Fact]
    public void CandidatesFollowSpecOrder() =>
        Assert.Equal(
            new[] { "h264_amf", "h264_amf", "h264_qsv", "h264_qsv", "h264_nvenc", "h264_nvenc", "h264_mf", "libx264" },
            EncoderProfiles.Candidates.Select(p => p.Codec));

    [Fact]
    public void BuildsMonitorCapture()
    {
        var args = FfmpegArguments.ForMonitor(Qsv, 1, 60);
        Assert.Equal(
            "-hide_banner -loglevel warning -f lavfi -i ddagrab=output_idx=1:framerate=60:draw_mouse=1 " +
            "-vf hwmap=derive_device=qsv,format=qsv -c:v h264_qsv -low_power 0 -look_ahead 0 -async_depth 1 " +
            "-bf 0 -g 120 -b:v 12M -maxrate 20M -bufsize 4M -bsf:v h264_metadata=aud=insert -f h264 pipe:1",
            string.Join(' ', args));
    }

    [Fact]
    public void ProfileWithoutFilterHasNoVf()
    {
        var amf = EncoderProfiles.Candidates.First(p => p.Codec == "h264_amf");
        Assert.DoesNotContain("-vf", FfmpegArguments.ForMonitor(amf, 0, 60));
    }

    [Fact]
    public void ProbeEncodesThreeFramesToNull()
    {
        var args = string.Join(' ', FfmpegArguments.Probe(Qsv, 0));
        Assert.Contains("-i ddagrab=output_idx=0:framerate=60:draw_mouse=1", args);
        Assert.EndsWith("-frames:v 3 -f null -", args);
    }

    [Fact]
    public void FindsFfmpegInPath()
    {
        var dir = Directory.CreateTempSubdirectory();
        File.WriteAllText(Path.Combine(dir.FullName, "ffmpeg.exe"), "");
        Assert.Equal(Path.Combine(dir.FullName, "ffmpeg.exe"), FfmpegLocator.Find($@"C:\nao-existe;{dir.FullName}"));
        Assert.Null(FfmpegLocator.Find(@"C:\nao-existe"));
    }
}
