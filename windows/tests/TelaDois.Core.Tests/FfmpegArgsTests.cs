using TelaDois.Core.Link;
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests;

public class FfmpegArgsTests
{
    private static readonly VideoConfig Phone = new(2556, 1179, 60);

    [Fact]
    public void AmfCapturesTheChosenOutputAsNv12WithoutBFramesAndOneSecondGop()
    {
        var args = string.Join(' ', FfmpegArgs.Desktop(1, Phone, "h264_amf"));
        Assert.Contains("ddagrab=output_idx=1:framerate=60:draw_mouse=1,hwdownload,format=bgra,format=nv12", args);
        Assert.Contains("-c:v h264_amf -usage ultralowlatency -bf 0 -b:v 12M -g 60 -header_spacing 60", args);
        Assert.DoesNotContain("aud=insert", args); // o AMF já põe AUD
        Assert.EndsWith("-f h264 -", args);
    }

    [Fact]
    public void X264RepeatsHeadersAndInsertsAud()
    {
        var args = string.Join(' ', FfmpegArgs.Desktop(0, Phone, "libx264"));
        Assert.Contains("-c:v libx264 -preset ultrafast -tune zerolatency -profile:v main -bf 0 -b:v 12M -g 60 -x264-params repeat-headers=1", args);
        Assert.Contains("-bsf:v h264_metadata=aud=insert", args);
    }
}
