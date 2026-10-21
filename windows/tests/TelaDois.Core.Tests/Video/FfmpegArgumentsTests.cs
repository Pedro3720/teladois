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
            "-vf hwmap=derive_device=qsv,format=qsv,vpp_qsv=cw=trunc(iw/2)*2:ch=trunc(ih/2)*2 -c:v h264_qsv -low_power 0 -look_ahead 0 -async_depth 1 -preset veryfast " +
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

    // ---- EncodeSettings (Etapa 5d): padrões = linha de hoje -------------------------------------------------------

    /// <summary>A linha de hoje, montada à mão com os valores da base, para qualquer perfil.</summary>
    private static string TodayLine(EncoderProfile p, int output)
    {
        var parts = new List<string> { "-nostdin -hide_banner -loglevel warning -f lavfi -i ddagrab=output_idx=" + output + ":framerate=60:draw_mouse=1" };
        if (p.Filter.Length > 0)
        {
            parts.Add("-vf " + p.Filter);
        }
        parts.Add("-c:v " + p.Codec);
        parts.AddRange(p.Options);
        parts.Add("-bf 0 -g 120 -b:v 12M -maxrate 20M -bufsize 4M -bsf:v h264_metadata=aud=insert -f h264 pipe:1");
        return string.Join(' ', parts);
    }

    public static TheoryData<string> AllProfileIds()
    {
        var data = new TheoryData<string>();
        foreach (var p in EncoderProfiles.Candidates)
        {
            data.Add(p.Id);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(AllProfileIds))]
    public void DefaultSettingsProduceTodaysLine(string id)
    {
        var p = EncoderProfiles.Candidates.Single(x => x.Id == id);
        var today = TodayLine(p, 2);
        Assert.Equal(today, string.Join(' ', FfmpegArguments.ForMonitor(p, 2, 60)));
        Assert.Equal(today, string.Join(' ', FfmpegArguments.ForMonitor(p, 2, 60, EncodeSettings.Default)));
        Assert.Equal(today, string.Join(' ', FfmpegArguments.ForMonitor(p, 2, 60, new EncodeSettings())));
    }

    /// <summary>Só o qsv-hwmap leva <c>-preset veryfast</c> (provisório, bancada da 5d); os outros perfis não mudam.</summary>
    [Fact]
    public void OnlyQsvHwmapCarriesPresetVeryfast() =>
        Assert.Equal(
            ["qsv-hwmap"],
            EncoderProfiles.Candidates.Where(p => p.Options.Contains("-preset") && p.Codec == "h264_qsv").Select(p => p.Id));

    [Fact]
    public void ProbeWithDefaultSettingsIsUnchanged()
    {
        var p = Qsv;
        Assert.Equal(
            FfmpegArguments.Probe(p, 0),
            FfmpegArguments.Probe(p, 0, EncodeSettings.Default));
    }

    [Fact]
    public void GopSecondsScalesTheKeyframeInterval()
    {
        var args = string.Join(' ', FfmpegArguments.ForMonitor(Qsv, 2, 60, new EncodeSettings { GopSeconds = 4 }));
        Assert.Contains(" -g 240 ", args);
    }

    [Fact]
    public void RatesComeFromTheSettings()
    {
        var args = string.Join(' ', FfmpegArguments.ForMonitor(Qsv, 2, 60, new EncodeSettings { Bitrate = "8M", Maxrate = "12M", Bufsize = "200k" }));
        Assert.Contains("-b:v 8M -maxrate 12M -bufsize 200k", args);
    }

    [Fact]
    public void GrabOptionsGoAtTheEndOfTheDdagrabGraph()
    {
        var s = new EncodeSettings { GrabOptions = ":dup_frames=0" };
        Assert.Contains("-i ddagrab=output_idx=2:framerate=60:draw_mouse=1:dup_frames=0 -vf", string.Join(' ', FfmpegArguments.ForMonitor(Qsv, 2, 60, s)));
        Assert.Contains("-i ddagrab=output_idx=2:framerate=60:draw_mouse=1:dup_frames=0 -vf", string.Join(' ', FfmpegArguments.Probe(Qsv, 2, s)));
    }

    [Fact]
    public void OverrideReplacesTheProfileOptionInPlace()
    {
        var s = new EncodeSettings { CodecOptionOverrides = new Dictionary<string, string> { ["-low_power"] = "1" } };
        var args = string.Join(' ', FfmpegArguments.ForMonitor(Qsv, 2, 60, s));
        Assert.Contains("-c:v h264_qsv -low_power 1 -look_ahead 0 -async_depth 1 -preset veryfast -bf 0", args);
        Assert.DoesNotContain("-low_power 0", args);
    }

    [Fact]
    public void UnknownOverrideKeyIsAppendedFollowedByExtraOptions()
    {
        var s = new EncodeSettings
        {
            CodecOptionOverrides = new Dictionary<string, string> { ["-rdo"] = "1" },
            ExtraCodecOptions = ["-int_ref_type", "vertical"],
        };
        var args = string.Join(' ', FfmpegArguments.ForMonitor(Qsv, 2, 60, s));
        Assert.Contains("-c:v h264_qsv -low_power 0 -look_ahead 0 -async_depth 1 -preset veryfast -rdo 1 -int_ref_type vertical -bf 0", args);
    }

    [Fact]
    public void OverridesAndExtrasApplyToTheProbeToo()
    {
        var s = new EncodeSettings { CodecOptionOverrides = new Dictionary<string, string> { ["-low_power"] = "1" }, ExtraCodecOptions = ["-x", "y"] };
        var args = string.Join(' ', FfmpegArguments.Probe(Qsv, 0, s));
        Assert.Contains("-low_power 1 -look_ahead 0 -async_depth 1 -preset veryfast -x y -frames:v 3", args);
    }

    [Fact]
    public void ScaleToOnQsvHwmapGoesIntoVppQsv()
    {
        var s = new EncodeSettings { ScaleTo = (1920, 886) };
        var args = FfmpegArguments.ForMonitor(Qsv, 2, 60, s);
        Assert.Contains("hwmap=derive_device=qsv,format=qsv,vpp_qsv=cw=trunc(iw/2)*2:ch=trunc(ih/2)*2:w=1920:h=886", args);
        Assert.Equal((1920, 886), Qsv.EncodedSize(2556, 1179, s));
    }

    [Fact]
    public void ScaleToOnDownloadProfilesAddsScaleAfterTheCrop()
    {
        var x264 = EncoderProfiles.Candidates.Single(p => p.Id == "x264");
        var s = new EncodeSettings { ScaleTo = (1920, 886) };
        var args = FfmpegArguments.ForMonitor(x264, 2, 60, s);
        Assert.Contains("hwdownload,format=bgra,crop=trunc(iw/2)*2:trunc(ih/2)*2,scale=1920:886,format=yuv420p", args);
        Assert.Equal((1920, 886), x264.EncodedSize(2556, 1179, s));
    }

    [Fact]
    public void ScaleToIsRoundedDownToEven()
    {
        var s = new EncodeSettings { ScaleTo = (1705, 787) };
        Assert.Equal((1704, 786), Qsv.EncodedSize(2556, 1179, s));
        Assert.Contains(":w=1704:h=786", string.Join(' ', FfmpegArguments.ForMonitor(Qsv, 2, 60, s)));
    }

    /// <summary>amf-d3d11 e nvenc-d3d11 não têm filtro (não dá para provar um aqui): ScaleTo é ignorado.</summary>
    [Theory]
    [InlineData("amf-d3d11")]
    [InlineData("nvenc-d3d11")]
    public void ScaleToIsIgnoredByProfilesWithoutFilter(string id)
    {
        var p = EncoderProfiles.Candidates.Single(x => x.Id == id);
        var s = new EncodeSettings { ScaleTo = (1920, 886) };
        Assert.Equal(FfmpegArguments.ForMonitor(p, 2, 60), FfmpegArguments.ForMonitor(p, 2, 60, s));
        Assert.Equal((2556, 1179), p.EncodedSize(2556, 1179, s));
    }

    [Fact]
    public void InputOptionsGoBeforeTheLavfiInput()
    {
        var s = new EncodeSettings { InputOptions = ["-fflags", "nobuffer"] };
        Assert.Contains("-loglevel warning -fflags nobuffer -f lavfi -i ddagrab=", string.Join(' ', FfmpegArguments.ForMonitor(Qsv, 2, 60, s)));
        Assert.Contains("-loglevel warning -fflags nobuffer -f lavfi -i ddagrab=", string.Join(' ', FfmpegArguments.Probe(Qsv, 2, s)));
    }

    [Fact]
    public void OutputOptionsGoAfterTheCodecOptions()
    {
        var s = new EncodeSettings { OutputOptions = ["-fps_mode", "passthrough"] };
        Assert.Contains("-preset veryfast -fps_mode passthrough -bf 0 -g 120", string.Join(' ', FfmpegArguments.ForMonitor(Qsv, 2, 60, s)));
        Assert.Contains("-preset veryfast -fps_mode passthrough -frames:v 3 -f null -", string.Join(' ', FfmpegArguments.Probe(Qsv, 2, s)));
    }

    [Fact]
    public void GrabFpsCapturesFasterAndBringsTheRateBackForTheEncoder()
    {
        var s = new EncodeSettings { GrabFps = 120, GrabOptions = ":dup_frames=0" };
        var args = string.Join(' ', FfmpegArguments.ForMonitor(Qsv, 2, 60, s));
        Assert.Contains("-i ddagrab=output_idx=2:framerate=120:draw_mouse=1:dup_frames=0,fps=60 -vf hwmap=", args);
        Assert.Contains(" -g 120 ", args);
        Assert.Contains("-i ddagrab=output_idx=2:framerate=120:draw_mouse=1:dup_frames=0,fps=60 -vf hwmap=", string.Join(' ', FfmpegArguments.Probe(Qsv, 2, s)));
    }

    [Fact]
    public void FilterComplexRunsCaptureAndProfileFilterInOneGraphWithoutLavfiInput()
    {
        var s = new EncodeSettings { FilterComplex = true };
        var args = FfmpegArguments.ForMonitor(Qsv, 2, 60, s);
        Assert.Equal(
            "-nostdin -hide_banner -loglevel warning -filter_complex ddagrab=output_idx=2:framerate=60:draw_mouse=1," +
            "hwmap=derive_device=qsv,format=qsv,vpp_qsv=cw=trunc(iw/2)*2:ch=trunc(ih/2)*2 -c:v h264_qsv -low_power 0 -look_ahead 0 -async_depth 1 -preset veryfast " +
            "-bf 0 -g 120 -b:v 12M -maxrate 20M -bufsize 4M -bsf:v h264_metadata=aud=insert -f h264 pipe:1",
            string.Join(' ', args));
        Assert.Contains("-filter_complex ddagrab=output_idx=2:framerate=60:draw_mouse=1,hwmap=", string.Join(' ', FfmpegArguments.Probe(Qsv, 2, s)));
    }

    [Theory]
    [MemberData(nameof(AllProfileIds))]
    public void FilterComplexOnlyMovesTheInputAndFilter(string id)
    {
        var p = EncoderProfiles.Candidates.Single(x => x.Id == id);
        var args = FfmpegArguments.ForMonitor(p, 2, 60, new EncodeSettings { FilterComplex = true });
        Assert.DoesNotContain("-i", args);
        Assert.DoesNotContain("-vf", args);
        Assert.DoesNotContain("lavfi", args);
        var graph = p.Filter.Length > 0 ? $"ddagrab=output_idx=2:framerate=60:draw_mouse=1,{p.Filter}" : "ddagrab=output_idx=2:framerate=60:draw_mouse=1";
        Assert.Equal(graph, args[args.ToList().IndexOf("-filter_complex") + 1]);
        var today = FfmpegArguments.ForMonitor(p, 2, 60);
        Assert.Equal(today.SkipWhile(a => a != "-c:v"), args.SkipWhile(a => a != "-c:v"));
    }

    [Fact]
    public void EncodeWithSettingsKeepsFrameCount()
    {
        var args = string.Join(' ', FfmpegArguments.Encode(Qsv, "testsrc=size=64x64:rate=30", 30, frames: 5, settings: new EncodeSettings { GopSeconds = 1 }));
        Assert.Contains("-g 30 ", args);
        Assert.Contains("-frames:v 5", args);
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
