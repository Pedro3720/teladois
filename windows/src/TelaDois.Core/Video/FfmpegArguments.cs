namespace TelaDois.Core.Video;

public static class FfmpegArguments
{
    public static IReadOnlyList<string> ForMonitor(EncoderProfile profile, int outputIndex, ushort fps) =>
        Encode(profile, DdaGrab(outputIndex, fps), fps);

    public static IReadOnlyList<string> Encode(EncoderProfile profile, string inputGraph, ushort fps, int? frames = null)
    {
        var args = Head(profile, inputGraph);
        args.AddRange(["-bf", "0", "-g", (fps * 2).ToString(), "-b:v", "12M", "-maxrate", "20M", "-bufsize", "4M"]);
        if (frames is { } count)
        {
            args.AddRange(["-frames:v", count.ToString()]);
        }
        args.AddRange(["-bsf:v", "h264_metadata=aud=insert", "-f", "h264", "pipe:1"]);
        return args;
    }

    /// <summary>Codifica 3 imagens do monitor e descarta: código 0 = a compressão funciona neste PC.</summary>
    public static IReadOnlyList<string> Probe(EncoderProfile profile, int outputIndex)
    {
        var args = Head(profile, DdaGrab(outputIndex, 60));
        args.AddRange(["-frames:v", "3", "-f", "null", "-"]);
        return args;
    }

    private static string DdaGrab(int outputIndex, ushort fps) => $"ddagrab=output_idx={outputIndex}:framerate={fps}:draw_mouse=1";

    private static List<string> Head(EncoderProfile profile, string inputGraph)
    {
        var args = new List<string> { "-nostdin", "-hide_banner", "-loglevel", "warning", "-f", "lavfi", "-i", inputGraph };
        if (profile.Filter.Length > 0)
        {
            args.AddRange(["-vf", profile.Filter]);
        }
        args.AddRange(["-c:v", profile.Codec]);
        args.AddRange(profile.Options);
        return args;
    }
}
