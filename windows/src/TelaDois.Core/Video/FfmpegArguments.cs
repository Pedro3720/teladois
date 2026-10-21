namespace TelaDois.Core.Video;

public static class FfmpegArguments
{
    public static IReadOnlyList<string> ForMonitor(EncoderProfile profile, int outputIndex, ushort fps, EncodeSettings? settings = null) =>
        Encode(profile, DdaGrab(outputIndex, fps, settings), fps, settings: settings);

    public static IReadOnlyList<string> Encode(EncoderProfile profile, string inputGraph, ushort fps, int? frames = null, EncodeSettings? settings = null)
    {
        settings ??= EncodeSettings.Default;
        var args = Head(profile, inputGraph, settings);
        args.AddRange(["-bf", "0", "-g", (fps * settings.GopSeconds).ToString(), "-b:v", settings.Bitrate, "-maxrate", settings.Maxrate, "-bufsize", settings.Bufsize]);
        if (frames is { } count)
        {
            args.AddRange(["-frames:v", count.ToString()]);
        }
        args.AddRange(["-bsf:v", "h264_metadata=aud=insert", "-f", "h264", "pipe:1"]);
        return args;
    }

    /// <summary>Codifica 3 imagens do monitor e descarta: código 0 = a compressão funciona neste PC.</summary>
    public static IReadOnlyList<string> Probe(EncoderProfile profile, int outputIndex, EncodeSettings? settings = null)
    {
        var args = Head(profile, DdaGrab(outputIndex, 60, settings), settings);
        args.AddRange(["-frames:v", "3", "-f", "null", "-"]);
        return args;
    }

    /// <remarks>
    /// Com <see cref="EncodeSettings.GrabFps"/> o ddagrab captura nessa taxa e o filtro <c>fps</c> devolve a da
    /// compressão. O <c>fps</c> só mexe nos tempos (<c>AVFILTER_FLAG_METADATA_ONLY</c>, sem lista de formatos em
    /// libavfilter/vf_fps.c do FFmpeg 9), então aceita as superfícies D3D11 do ddagrab antes do hwmap/hwdownload; ele
    /// segura 1 imagem de entrada (precisa da seguinte para decidir, <c>frames[2]</c>): ~8 ms a 120/s.
    /// </remarks>
    private static string DdaGrab(int outputIndex, ushort fps, EncodeSettings? settings) =>
        settings?.GrabFps is { } grabFps
            ? $"ddagrab=output_idx={outputIndex}:framerate={grabFps}:draw_mouse=1{settings.GrabOptions},fps={fps}"
            : $"ddagrab=output_idx={outputIndex}:framerate={fps}:draw_mouse=1{settings?.GrabOptions}";

    private static List<string> Head(EncoderProfile profile, string inputGraph, EncodeSettings? settings)
    {
        var args = new List<string> { "-nostdin", "-hide_banner", "-loglevel", "warning" };
        args.AddRange(settings?.InputOptions ?? []);
        var filter = profile.FilterFor(settings);
        if (settings?.FilterComplex == true)
        {
            // Um grafo só de fonte roda direto no -filter_complex (exemplo do ddagrab em doc/filters.texi do FFmpeg 9:
            // "-filter_complex ddagrab=output_idx=1:framerate=60,hwdownload,format=bgra"); -vf e -filter_complex não
            // se juntam na mesma saída, por isso o filtro do perfil vai no fim do grafo.
            args.AddRange(["-filter_complex", filter.Length > 0 ? $"{inputGraph},{filter}" : inputGraph]);
        }
        else
        {
            args.AddRange(["-f", "lavfi", "-i", inputGraph]);
            if (filter.Length > 0)
            {
                args.AddRange(["-vf", filter]);
            }
        }
        args.AddRange(["-c:v", profile.Codec]);
        args.AddRange(profile.OptionsFor(settings));
        args.AddRange(settings?.OutputOptions ?? []);
        return args;
    }
}
