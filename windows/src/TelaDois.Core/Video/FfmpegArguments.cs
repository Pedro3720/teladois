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

    private static string DdaGrab(int outputIndex, ushort fps, EncodeSettings? settings) =>
        $"ddagrab=output_idx={outputIndex}:framerate={fps}:draw_mouse=1{settings?.GrabOptions}";

    private static List<string> Head(EncoderProfile profile, string inputGraph, EncodeSettings? settings)
    {
        var args = new List<string> { "-nostdin", "-hide_banner", "-loglevel", "warning", "-f", "lavfi", "-i", inputGraph };
        var filter = profile.FilterFor(settings);
        if (filter.Length > 0)
        {
            args.AddRange(["-vf", filter]);
        }
        args.AddRange(["-c:v", profile.Codec]);
        args.AddRange(profile.OptionsFor(settings));
        return args;
    }
}
