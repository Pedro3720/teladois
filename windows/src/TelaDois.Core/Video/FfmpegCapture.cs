using TelaDois.Core.Link;

namespace TelaDois.Core.Video;

/// <summary>
/// Roda o FFmpeg e põe as imagens na <see cref="FrameQueue"/>. Se o processo morrer, reinicia após a espera
/// seguinte de <c>backoff</c> (1 s, 2 s, 5 s), que volta ao início quando chegam imagens de novo.
/// </summary>
public sealed class FfmpegCapture : IVideoFeed
{
    private static readonly TimeSpan[] DefaultBackoff = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5)];

    private readonly FrameQueue _queue;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _pump;

    public FfmpegCapture(IProcessRunner runner, string ffmpegPath, IReadOnlyList<string> arguments, VideoConfig config, Action<string> log, IReadOnlyList<TimeSpan>? backoff = null, int queueCapacity = 3)
    {
        Config = config;
        _queue = new FrameQueue(queueCapacity);
        var waits = backoff ?? DefaultBackoff;
        _pump = Task.Run(() => PumpAsync(runner, ffmpegPath, arguments, log, waits, _cts.Token));
    }

    public VideoConfig Config { get; }

    public long Dropped => _queue.Dropped;

    public Task<AccessUnit> NextAsync(CancellationToken ct) => _queue.TakeAsync(ct);

    private async Task PumpAsync(IProcessRunner runner, string ffmpegPath, IReadOnlyList<string> arguments, Action<string> log, IReadOnlyList<TimeSpan> waits, CancellationToken ct)
    {
        var attempt = 0;
        while (!ct.IsCancellationRequested)
        {
            var sawFrames = false;
            await using (var process = runner.Start(ffmpegPath, arguments))
            {
                try
                {
                    await foreach (var unit in new AccessUnitReader(process.Output).ReadAllAsync(ct))
                    {
                        sawFrames = true;
                        _queue.Add(unit);
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
                catch (IOException e)
                {
                    log($"leitura do FFmpeg falhou: {e.Message}");
                }
                if (ct.IsCancellationRequested)
                {
                    return;
                }
                log($"FFmpeg terminou: {process.RecentErrors}");
            }
            attempt = sawFrames ? 0 : attempt;
            var wait = waits[Math.Min(attempt, waits.Count - 1)];
            attempt++;
            try
            {
                await Task.Delay(wait, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        try
        {
            await _pump;
        }
        catch (OperationCanceledException)
        {
        }
        _cts.Dispose();
    }
}
