using System.ComponentModel;
using TelaDois.Core.Link;
using TelaDois.Core.Tests.Fakes;
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Video;

public class FfmpegCaptureTests
{
    private static readonly VideoConfig Config = new(64, 64, 30);
    private static readonly TimeSpan[] FastBackoff = [TimeSpan.FromMilliseconds(10)];
    private static readonly TimeSpan[] NoRestart = [TimeSpan.FromHours(1)];
    private static readonly TimeSpan Generous = TimeSpan.FromSeconds(5);

    private static Action<string> Collect(List<string> log) => line =>
    {
        lock (log)
        {
            log.Add(line);
        }
    };

    private static string[] Snapshot(List<string> log)
    {
        lock (log)
        {
            return [.. log];
        }
    }

    [Fact]
    public async Task DeliversFramesAndRestartsWhenFfmpegExits()
    {
        var log = new List<string>();
        var runner = new FakeProcessRunner(_ => new FakeProcess(1, AccessUnitReaderTests.Fixture(), "caiu"));
        // Idade máxima folgada: o teste não depende de ler cada rajada antes de a próxima chegar.
        await using (var capture = new FfmpegCapture(runner, "ffmpeg", ["-x"], Config, Collect(log), FastBackoff, maxQueueAge: TimeSpan.FromMinutes(1)))
        {
            Assert.Equal(Config, capture.Config);
            var frames = new List<AccessUnit>();
            using var cts = new CancellationTokenSource(Generous);
            for (var i = 0; i < 8; i++)
            {
                frames.Add(await capture.NextAsync(cts.Token));
            }
            Assert.True(frames[0].IsKeyframe);
            Assert.True(runner.Started.Count >= 2, "deveria ter reiniciado o FFmpeg");
        }
        Assert.Contains(Snapshot(log), line => line.Contains("caiu"));
        Assert.Equal(runner.Started.Count, runner.Disposed);
    }

    [Fact]
    public async Task MetersCountTheFramesReadFromFfmpeg()
    {
        var runner = new FakeProcessRunner(_ => new FakeProcess(0, AccessUnitReaderTests.Fixture()));
        await using var capture = new FfmpegCapture(runner, "ffmpeg", ["-x"], Config, _ => { }, NoRestart, maxQueueAge: TimeSpan.FromMinutes(1));
        using var cts = new CancellationTokenSource(Generous);
        var taken = 0;
        while (taken < 3)
        {
            await capture.NextAsync(cts.Token);
            taken++;
        }
        var meters = ((IVideoFeed)capture).Meters;
        Assert.NotNull(meters);
        var snapshot = meters.Take();
        Assert.True(snapshot.Read >= 3, $"lidas {snapshot.Read}");
        Assert.Equal(3, snapshot.Queue.Count);
    }

    [Fact]
    public async Task DisposingBeforeTheFirstFrameDoesNotReportAMissingPicture()
    {
        // Desligar/tirar o cabo antes da 1ª imagem é decisão do usuário: não deve fazer esquecer a compressão.
        var stdout = new BlockingStream();
        var reports = 0;
        var runner = new FakeProcessRunner(_ => new FakeProcess(0, hangs: true, stream: stdout));
        var capture = new FfmpegCapture(runner, "ffmpeg", ["-x"], Config, _ => { }, NoRestart, endedWithoutFrames: () => Interlocked.Increment(ref reports));
        await stdout.ReadStarted.Task.WaitAsync(Generous);
        await capture.DisposeAsync();
        await capture.DisposeAsync();
        Assert.Equal(0, reports);
    }

    [Fact]
    public async Task ReportsOnceWhenFfmpegEndsWithoutEverProducingAPicture()
    {
        var reports = 0;
        var reported = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runner = new FakeProcessRunner(_ => new FakeProcess(1, errors: "falhou"));
        var capture = new FfmpegCapture(runner, "ffmpeg", ["-x"], Config, _ => { }, FastBackoff, endedWithoutFrames: () =>
        {
            Interlocked.Increment(ref reports);
            reported.TrySetResult();
        });
        await reported.Task.WaitAsync(Generous);
        await Task.Delay(100); // várias reexecuções seguintes: continua sendo uma só
        await capture.DisposeAsync();
        Assert.Equal(1, reports);
    }

    /// <summary>
    /// A 1ª execução sem imagem é comum logo depois de ligar o monitor (ddagrab 887a0026) e passa: não faz esquecer a
    /// compressão. A 2ª seguida, sim (o prazo da 1ª imagem da sessão cobre o caso que trava sem terminar).
    /// </summary>
    [Fact]
    public async Task OnlyTheSecondRunInARowWithoutPicturesIsReported()
    {
        var reports = 0;
        var secondReading = new BlockingStream();
        var starts = 0;
        var runner = new FakeProcessRunner(_ => Interlocked.Increment(ref starts) == 1
            ? new FakeProcess(1, errors: "887a0026")
            : new FakeProcess(0, hangs: true, stream: secondReading));
        var capture = new FfmpegCapture(runner, "ffmpeg", ["-x"], Config, _ => { }, FastBackoff, endedWithoutFrames: () => Interlocked.Increment(ref reports));
        // A 2ª execução só começa depois de a 1ª ter sido tratada (e, se fosse, avisada).
        await secondReading.ReadStarted.Task.WaitAsync(Generous);
        Assert.Equal(0, reports);
        await capture.DisposeAsync();

        var thirdReading = new BlockingStream();
        starts = 0;
        runner = new FakeProcessRunner(_ => Interlocked.Increment(ref starts) <= 2
            ? new FakeProcess(1, errors: "falhou")
            : new FakeProcess(0, hangs: true, stream: thirdReading));
        capture = new FfmpegCapture(runner, "ffmpeg", ["-x"], Config, _ => { }, FastBackoff, endedWithoutFrames: () => Interlocked.Increment(ref reports));
        await thirdReading.ReadStarted.Task.WaitAsync(Generous);
        Assert.Equal(1, reports);
        await capture.DisposeAsync();
    }

    [Fact]
    public async Task ACallbackThatThrowsNeitherStopsThePumpNorEscapes()
    {
        var log = new List<string>();
        var reading = new BlockingStream();
        var starts = 0;
        var runner = new FakeProcessRunner(_ => Interlocked.Increment(ref starts) <= 2
            ? new FakeProcess(1, errors: "falhou")
            : new FakeProcess(0, hangs: true, stream: reading));
        var capture = new FfmpegCapture(runner, "ffmpeg", ["-x"], Config, Collect(log), FastBackoff,
            endedWithoutFrames: () => throw new InvalidOperationException("o aviso quebrou"));
        await reading.ReadStarted.Task.WaitAsync(Generous); // a 3ª execução começou: o pump seguiu
        capture.FirstFrameMissed(); // já avisou uma vez: não chama de novo, não lança
        await capture.DisposeAsync().AsTask().WaitAsync(Generous);
        Assert.Contains(Snapshot(log), line => line.Contains("o aviso quebrou", StringComparison.Ordinal));

        var stdout = new BlockingStream();
        var hanging = new FfmpegCapture(new FakeProcessRunner(_ => new FakeProcess(0, hangs: true, stream: stdout)), "ffmpeg", ["-x"], Config, Collect(log), NoRestart,
            endedWithoutFrames: () => throw new InvalidOperationException("de novo"));
        await stdout.ReadStarted.Task.WaitAsync(Generous);
        hanging.FirstFrameMissed(); // não lança
        await hanging.DisposeAsync();
        Assert.Contains(Snapshot(log), line => line.Contains("de novo", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FirstFrameMissedReportsOnceWhileNoPictureCameAndNeverAfterOne()
    {
        var reports = 0;
        var stdout = new BlockingStream();
        var hanging = new FfmpegCapture(new FakeProcessRunner(_ => new FakeProcess(0, hangs: true, stream: stdout)), "ffmpeg", ["-x"], Config, _ => { }, NoRestart,
            endedWithoutFrames: () => Interlocked.Increment(ref reports));
        await stdout.ReadStarted.Task.WaitAsync(Generous);
        hanging.FirstFrameMissed();
        hanging.FirstFrameMissed();
        await hanging.DisposeAsync();
        Assert.Equal(1, reports);

        var delivering = new FfmpegCapture(new FakeProcessRunner(_ => new FakeProcess(0, AccessUnitReaderTests.Fixture())), "ffmpeg", ["-x"], Config, _ => { }, NoRestart,
            endedWithoutFrames: () => Interlocked.Increment(ref reports));
        await delivering.NextAsync(new CancellationTokenSource(Generous).Token);
        delivering.FirstFrameMissed();
        await delivering.DisposeAsync();
        Assert.Equal(1, reports);
    }

    [Fact]
    public async Task LogsOnceWhenTheCableFallsBehind()
    {
        // A 1ª imagem (completa) fica na fila sem ninguém ler; as outras 3 chegam bem depois de 1 ms.
        var bytes = AccessUnitReaderTests.Fixture();
        var auds = H264.ScanNals(bytes, bytes.Length).Where(n => n.Type == H264.Aud).Select(n => n.Start).ToArray();
        var split = auds[1] + 6; // inclui o AUD da 2ª imagem inteiro: o leitor já entrega a 1ª
        var stdout = new GatedStream(bytes[..split], bytes[split..]);
        var log = new List<string>();
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runner = new FakeProcessRunner(_ => new FakeProcess(1, stream: stdout));
        var capture = new FfmpegCapture(runner, "ffmpeg", ["-x"], Config, line =>
        {
            Collect(log)(line);
            if (line.StartsWith("FFmpeg terminou:"))
            {
                finished.TrySetResult();
            }
        }, NoRestart, maxQueueAge: TimeSpan.FromMilliseconds(1));
        await stdout.AtGate.Task.WaitAsync(Generous); // a 1ª imagem já está na fila
        await Task.Delay(30);                          // e fica mais velha que 1 ms
        stdout.Open.TrySetResult();
        await finished.Task.WaitAsync(Generous);
        Assert.Equal(4, capture.Dropped);
        var warning = Assert.Single(Snapshot(log), line => line.StartsWith("cabo atrasado:"));
        Assert.Equal("cabo atrasado: descartando até a próxima imagem completa (2 descartadas até agora)", warning);
        await capture.DisposeAsync();
    }

    [Fact]
    public async Task KeepsTryingWhenFfmpegFailsToStart()
    {
        var log = new List<string>();
        var starts = 0;
        var runner = new FakeProcessRunner(_ => Interlocked.Increment(ref starts) <= 2
            ? throw new Win32Exception(2, "arquivo não encontrado")
            : new FakeProcess(1, AccessUnitReaderTests.Fixture()));
        var capture = new FfmpegCapture(runner, "ffmpeg", ["-x"], Config, Collect(log), FastBackoff);
        using var cts = new CancellationTokenSource(Generous);
        Assert.True((await capture.NextAsync(cts.Token)).IsKeyframe);
        await capture.DisposeAsync(); // não lança
        Assert.True(runner.Started.Count >= 3, "deveria ter tentado de novo depois das 2 falhas");
        Assert.Equal(2, Snapshot(log).Count(line => line.StartsWith("FFmpeg não iniciou:") && line.Contains("arquivo não encontrado")));
        Assert.Equal(runner.Started.Count - 2, runner.Disposed);
    }

    [Fact]
    public async Task KeepsTryingWhenReadingFails()
    {
        var log = new List<string>();
        var starts = 0;
        var runner = new FakeProcessRunner(_ => Interlocked.Increment(ref starts) == 1
            ? new FakeProcess(1, stream: new FailingStream(new InvalidOperationException("pipe quebrado")))
            : new FakeProcess(1, AccessUnitReaderTests.Fixture()));
        var capture = new FfmpegCapture(runner, "ffmpeg", ["-x"], Config, Collect(log), FastBackoff);
        using var cts = new CancellationTokenSource(Generous);
        Assert.True((await capture.NextAsync(cts.Token)).IsKeyframe);
        await capture.DisposeAsync();
        Assert.Contains(Snapshot(log), line => line.StartsWith("leitura do FFmpeg falhou:") && line.Contains("pipe quebrado"));
        Assert.Equal(runner.Started.Count, runner.Disposed);
    }

    [Fact]
    public async Task DisposeReleasesWhoeverIsWaitingForAFrame()
    {
        // O FFmpeg sai sem mandar nada e a próxima tentativa demora 1 h: quem espera uma imagem fica esperando.
        var runner = new FakeProcessRunner(_ => new FakeProcess(1));
        var capture = new FfmpegCapture(runner, "ffmpeg", ["-x"], Config, _ => { }, NoRestart);
        var waiting = capture.NextAsync(CancellationToken.None);
        Assert.False(waiting.IsCompleted);
        await capture.DisposeAsync().AsTask().WaitAsync(Generous);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting.WaitAsync(TimeSpan.FromSeconds(1)));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => capture.NextAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task DisposeStopsAProcessWhosePipeReadIgnoresCancellation()
    {
        var stdout = new BlockingStream();
        var runner = new FakeProcessRunner(_ => new FakeProcess(0, stream: stdout));
        var capture = new FfmpegCapture(runner, "ffmpeg", ["-x"], Config, _ => { }, FastBackoff);
        await stdout.ReadStarted.Task.WaitAsync(Generous);
        await capture.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Single(runner.Started);
        Assert.Equal(1, runner.Disposed);
        Assert.True(stdout.IsDisposed);
    }

    [Fact]
    public async Task DisposeTwiceIsHarmless()
    {
        var capture = new FfmpegCapture(new FakeProcessRunner(_ => new FakeProcess(1)), "ffmpeg", ["-x"], Config, _ => { }, NoRestart);
        await capture.DisposeAsync();
        await capture.DisposeAsync();
    }

    [Fact]
    public async Task WaitGrowsBetweenFailuresAndResetsAfterFrames()
    {
        // Roteiro: 4 saídas sem imagens, 1 com imagens, 2 sem imagens; a 8ª fica aberta (fim do roteiro).
        var log = new List<string>();
        var last = new BlockingStream();
        var starts = 0;
        var runner = new FakeProcessRunner(_ => Interlocked.Increment(ref starts) switch
        {
            5 => new FakeProcess(1, AccessUnitReaderTests.Fixture()),
            8 => new FakeProcess(0, stream: last),
            _ => new FakeProcess(1),
        });
        TimeSpan[] waits = [TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(50)];
        var capture = new FfmpegCapture(runner, "ffmpeg", ["-x"], Config, Collect(log), waits);
        await last.ReadStarted.Task.WaitAsync(Generous);
        // Toda espera é registrada antes do Start seguinte: com a 8ª lendo, as 7 esperas já estão no log.
        var used = Snapshot(log)
            .Where(line => line.StartsWith("FFmpeg: nova tentativa em "))
            .Select(line => int.Parse(line["FFmpeg: nova tentativa em ".Length..].Split(' ')[0]))
            .ToArray();
        Assert.Equal(new[] { 10, 20, 50, 50, 10, 20, 50 }, used);
        Assert.Equal(8, runner.Started.Count);
        await capture.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task RealFfmpegProducesFrames()
    {
        var ffmpeg = FfmpegLocator.Find();
        if (ffmpeg is null)
        {
            return; // FFmpeg ausente nesta máquina: o teste com o FFmpeg real só roda onde ele está instalado.
        }
        var profile = new EncoderProfile("teste", "libx264", "format=yuv420p", ["-preset", "ultrafast", "-tune", "zerolatency"]);
        var args = FfmpegArguments.Encode(profile, "testsrc2=size=320x180:rate=30", 30, frames: 20);
        await using var capture = new FfmpegCapture(new SystemProcessRunner(), ffmpeg, args, new VideoConfig(320, 180, 30), _ => { });
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        // O FFmpeg codifica as 20 imagens mais rápido do que o teste lê: a fila pode descartar algumas.
        // Basta provar que a primeira é completa e que outras chegam.
        var first = await capture.NextAsync(cts.Token);
        Assert.True(first.IsKeyframe);
        Assert.NotEmpty((await capture.NextAsync(cts.Token)).Data);
    }
}
