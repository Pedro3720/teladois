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
            "-nostdin -hide_banner -loglevel warning -f lavfi -i ddagrab=output_idx=1:framerate=60:draw_mouse=1 " +
            "-vf hwmap=derive_device=qsv,format=qsv,vpp_qsv=cw=trunc(iw/2)*2:ch=trunc(ih/2)*2 -c:v h264_qsv -low_power 0 -look_ahead 0 -async_depth 1 " +
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

    /// <summary>x264 e Media Foundation recusam altura ímpar (2556×1179 do iPhone): corta 1 linha antes de converter.</summary>
    [Theory]
    [InlineData("amf-download", "format=nv12")]
    [InlineData("qsv-download", "format=nv12")]
    [InlineData("nvenc-download", "format=nv12")]
    [InlineData("mf", "format=nv12")]
    [InlineData("x264", "format=yuv420p")]
    public void SoftwareProfilesCropToEvenBeforeConverting(string id, string conversion)
    {
        var profile = EncoderProfiles.Candidates.Single(p => p.Id == id);
        Assert.Equal($"hwdownload,format=bgra,crop=trunc(iw/2)*2:trunc(ih/2)*2,{conversion}", profile.Filter);
        Assert.Contains(profile.Filter, FfmpegArguments.ForMonitor(profile, 0, 60));
        Assert.Contains(profile.Filter, FfmpegArguments.Probe(profile, 0));
    }

    /// <summary>Sem corte o h264_qsv arredonda 1179 para cima (1180, medido); o vpp_qsv corta na própria placa.</summary>
    [Fact]
    public void QsvHwmapCropsToEvenOnTheGpu() =>
        Assert.Equal("hwmap=derive_device=qsv,format=qsv,vpp_qsv=cw=trunc(iw/2)*2:ch=trunc(ih/2)*2", Qsv.Filter);

    [Theory]
    [InlineData("amf-download")]
    [InlineData("qsv-hwmap")]
    [InlineData("qsv-download")]
    [InlineData("nvenc-download")]
    [InlineData("mf")]
    [InlineData("x264")]
    public void CroppingProfilesEncodeTheEvenSize(string id)
    {
        var profile = EncoderProfiles.Candidates.Single(p => p.Id == id);
        Assert.Equal((2556, 1178), profile.EncodedSize(2556, 1179));
        Assert.Equal((1366, 768), profile.EncodedSize(1366, 768));
        Assert.Equal((1364, 768), profile.EncodedSize(1365, 769));
    }

    /// <summary>Sem placa AMD/NVIDIA aqui para provar um corte: ficam como estão e informam o tamanho do monitor.</summary>
    [Theory]
    [InlineData("amf-d3d11")]
    [InlineData("nvenc-d3d11")]
    public void UnfilteredGpuProfilesKeepTheMonitorSize(string id) =>
        Assert.Equal((2556, 1179), EncoderProfiles.Candidates.Single(p => p.Id == id).EncodedSize(2556, 1179));

    /// <summary>
    /// FFmpeg real: o perfil x264 inteiro (com o hwdownload) codifica 2556×1179. As imagens do testsrc sobem para a
    /// placa (D3D11) antes, como as do ddagrab chegam.
    /// </summary>
    [Fact]
    public async Task RealFfmpegX264EncodesTheOddIphoneSize()
    {
        if (FfmpegLocator.Find() is not { } ffmpeg)
        {
            return; // FFmpeg ausente nesta máquina: o teste com o FFmpeg real só roda onde ele está instalado.
        }
        var x264 = EncoderProfiles.Candidates.Single(p => p.Id == "x264");
        var uploaded = x264 with { Filter = "format=bgra,hwupload," + x264.Filter };
        string[] device = ["-init_hw_device", "d3d11va=d", "-filter_hw_device", "d"];
        var args = device.Concat(FfmpegArguments.Encode(uploaded, "testsrc=size=2556x1179:rate=60", 60, frames: 3)).ToArray();
        await using var process = new SystemProcessRunner().Start(ffmpeg, args);
        await process.Output.CopyToAsync(Stream.Null);
        var code = await process.WaitForExitAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(30));
        Assert.True(code == 0, process.RecentErrors);
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
