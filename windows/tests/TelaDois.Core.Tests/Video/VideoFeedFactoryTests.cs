using System.Collections.Concurrent;
using System.ComponentModel;
using TelaDois.Core.Display;
using TelaDois.Core.Link;
using TelaDois.Core.Tests.Fakes;
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Video;

public class VideoFeedFactoryTests
{
    private static readonly HelloInfo Hello = new(1, 2556, 1179);
    private static readonly DisplayOutput Main = new(0, @"\\.\DISPLAY2", 1920, 1080, true);
    private static readonly DisplayOutput Side = new(1, @"\\.\DISPLAY1", 1366, 768, false);

    [Fact]
    public async Task WithoutFfmpegReportsMissingAndReturnsNull()
    {
        var missing = 0;
        var factory = new VideoFeedFactory(null, new FakeProcessRunner(_ => new FakeProcess(0)), () => [Main], () => null, _ => { });
        factory.FfmpegMissing += () => missing++;
        Assert.Null(await factory.OpenAsync(Hello, CancellationToken.None));
        Assert.Equal(1, missing);
    }

    [Fact]
    public async Task CapturesChosenMonitorWithProbedEncoder()
    {
        var runner = new FakeProcessRunner(args => args.Contains("-frames:v") ? new FakeProcess(args.Contains("h264_mf") ? 0 : 1) : new FakeProcess(0, AccessUnitReaderTests.Fixture()));
        var factory = new VideoFeedFactory("ffmpeg", runner, () => [Main, Side], () => null, _ => { });
        await using var feed = await factory.OpenAsync(Hello, CancellationToken.None);
        Assert.NotNull(feed);
        Assert.Equal(new VideoConfig(1366, 768, 60), feed!.Config);
        await feed.NextAsync(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
        var capture = runner.Started.Last(a => !a.Contains("-frames:v"));
        Assert.Contains("ddagrab=output_idx=1:framerate=60:draw_mouse=1", capture);
        Assert.Contains("h264_mf", capture);
    }

    [Fact]
    public async Task NoMonitorReturnsNull() =>
        Assert.Null(await new VideoFeedFactory("ffmpeg", new FakeProcessRunner(_ => new FakeProcess(0)), () => [], () => null, _ => { }).OpenAsync(Hello, CancellationToken.None));

    [Fact]
    public async Task NoWorkingEncoderLogsAndReturnsNull()
    {
        var log = new ConcurrentQueue<string>();
        var factory = new VideoFeedFactory("ffmpeg", new FakeProcessRunner(_ => new FakeProcess(1)), () => [Main], () => null, log.Enqueue);
        Assert.Null(await factory.OpenAsync(Hello, CancellationToken.None));
        Assert.Contains("Segunda Tela indisponível: nenhuma compressão H.264 funcionou neste PC", log);
    }

    [Fact]
    public async Task FfmpegThatCannotStartLogsAndReturnsNull()
    {
        var log = new ConcurrentQueue<string>();
        var factory = new VideoFeedFactory("ffmpeg", new FakeProcessRunner(_ => throw new Win32Exception(2, "arquivo não encontrado")), () => [Main], () => null, log.Enqueue);
        Assert.Null(await factory.OpenAsync(Hello, CancellationToken.None));
        Assert.Contains("Segunda Tela indisponível: arquivo não encontrado", log);
    }

    [Fact]
    public async Task CancellationStillPropagates()
    {
        var factory = new VideoFeedFactory("ffmpeg", new FakeProcessRunner(_ => new FakeProcess(0)), () => [Main], () => null, _ => { });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => factory.OpenAsync(Hello, new CancellationToken(true)));
    }

    private static readonly DisplayOutput Virtual = new(2, @"\\.\DISPLAY3", 2556, 1179, false);

    /// <summary>Sondagem: só o h264_mf funciona; captura: entrega o vídeo de teste.</summary>
    private static FakeProcessRunner WorkingRunner() =>
        new(args => args.Contains("-frames:v") ? new FakeProcess(args.Contains("h264_mf") ? 0 : 1) : new FakeProcess(0, AccessUnitReaderTests.Fixture()));

    [Fact]
    public async Task ExtendsWithTheVirtualMonitorAndDetachesOnDispose()
    {
        var monitor = new FakeVirtualMonitor();
        var runner = WorkingRunner();
        var factory = new VideoFeedFactory("ffmpeg", runner, () => monitor.Attached ? new[] { Main, Virtual } : new[] { Main }, () => null, _ => { }, monitor);

        var feed = await factory.OpenAsync(Hello, CancellationToken.None);

        Assert.NotNull(feed);
        Assert.Equal(VideoKind.Extend, feed!.Kind);
        // O CONFIG informa o tamanho codificado: o h264_mf corta a linha ímpar (2556×1179 → 2556×1178).
        Assert.Equal(new VideoConfig(2556, 1178, 60), feed.Config);
        await feed.NextAsync(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
        Assert.Contains("ddagrab=output_idx=2:framerate=60:draw_mouse=1", runner.Started.Last(a => !a.Contains("-frames:v")));
        Assert.Equal(new[] { "PowerOn", "Attach" }, monitor.Calls); // ativa o dispositivo antes de pôr no desktop
        await feed.DisposeAsync();
        Assert.Equal(new[] { Virtual.DeviceName }, monitor.Detached);
        Assert.Equal(new[] { "PowerOn", "Attach", "Detach", "PowerOff" }, monitor.Calls);
    }

    [Fact]
    public async Task DeviceThatDoesNotPowerOnWithTheTasksPresentMirrorsWithoutTheDriverWarning()
    {
        // As tarefas existem, mas o monitor não apareceu depois de ativar: o driver não está "ausente".
        var monitor = new FakeVirtualMonitor { PowersOn = false, Installed = false };
        var warned = 0;
        var log = new ConcurrentQueue<string>();
        var factory = new VideoFeedFactory("ffmpeg", WorkingRunner(), () => [Main, Side], () => null, log.Enqueue, monitor);
        factory.DriverMissing += () => warned++;

        await using var feed = await factory.OpenAsync(Hello, CancellationToken.None);

        Assert.Equal(VideoKind.Mirror, feed!.Kind);
        Assert.Contains("o monitor virtual não ativou: duplicando um monitor", log);
        Assert.Equal(0, warned);
        // O dispositivo pode ativar depois do tempo de espera: desativa para o monitor não ficar listado.
        Assert.Equal(new[] { "PowerOn", "PowerOff" }, monitor.Calls);
    }

    [Fact]
    public async Task DriverThatRespondsButNeverShowsTheMonitorMirrorsWithoutWarningOrPowerOff()
    {
        // Driver instalado, sem tarefas (nada a desativar) e o monitor não aparece.
        var monitor = new FakeVirtualMonitor { PowersOn = false, CanPowerOff = false, Installed = true };
        var warned = 0;
        var log = new ConcurrentQueue<string>();
        var factory = new VideoFeedFactory("ffmpeg", WorkingRunner(), () => [Main, Side], () => null, log.Enqueue, monitor);
        factory.DriverMissing += () => warned++;

        await using var feed = await factory.OpenAsync(Hello, CancellationToken.None);

        Assert.Equal(VideoKind.Mirror, feed!.Kind);
        Assert.Contains("o driver do monitor virtual responde, mas o monitor não aparece no Windows: duplicando um monitor", log);
        Assert.Equal(0, warned);
        Assert.Equal(new[] { "PowerOn" }, monitor.Calls);
    }

    [Fact]
    public async Task CancellationDuringPowerOnPowersOffOnceAndPropagates()
    {
        var monitor = new FakeVirtualMonitor { PowerOnError = new OperationCanceledException() };
        var factory = new VideoFeedFactory("ffmpeg", WorkingRunner(), () => [Main, Side], () => null, _ => { }, monitor);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => factory.OpenAsync(Hello, new CancellationToken(true)));

        Assert.Equal(new[] { "PowerOn", "PowerOff" }, monitor.Calls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CancellationWhileCheckingTheTasksAfterPowerOnPowersOffOnceAndPropagates(bool powersOn)
    {
        // Ligou (ou a tarefa de ligar já rodou) e o cabo sai durante a consulta às tarefas: o dispositivo não fica ativo.
        using var cts = new CancellationTokenSource();
        var monitor = new FakeVirtualMonitor { PowersOn = powersOn, DuringCanPowerOff = cts.Cancel };
        var factory = new VideoFeedFactory("ffmpeg", WorkingRunner(), () => [Main, Side], () => null, _ => { }, monitor);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => factory.OpenAsync(Hello, cts.Token));

        Assert.Equal(new[] { "PowerOn", "PowerOff" }, monitor.Calls);
        Assert.False(monitor.Attached);
    }

    [Fact]
    public async Task PowerOnFailurePowersOffOnceAndReturnsNull()
    {
        var monitor = new FakeVirtualMonitor { PowerOnError = new IOException("o Agendador de Tarefas não respondeu") };
        var log = new ConcurrentQueue<string>();
        var factory = new VideoFeedFactory("ffmpeg", WorkingRunner(), () => [Main, Side], () => null, log.Enqueue, monitor);

        Assert.Null(await factory.OpenAsync(Hello, CancellationToken.None));

        Assert.Contains("Segunda Tela indisponível: o Agendador de Tarefas não respondeu", log);
        Assert.Equal(new[] { "PowerOn", "PowerOff" }, monitor.Calls);
    }

    [Fact]
    public async Task UnexpectedAttachFailurePowersOffOnceAndIsStillReported()
    {
        var monitor = new FakeVirtualMonitor { AttachError = new ArgumentException("resolução inválida") };
        var log = new ConcurrentQueue<string>();
        var factory = new VideoFeedFactory("ffmpeg", WorkingRunner(), () => [Main, Side], () => null, log.Enqueue, monitor);

        Assert.Null(await factory.OpenAsync(Hello, CancellationToken.None));

        Assert.Contains("Segunda Tela indisponível: resolução inválida", log);
        Assert.Equal(new[] { "PowerOn", "PowerOff" }, monitor.Calls);
    }

    [Fact]
    public async Task InstalledDriverWithoutTheTasksExtendsAndWarnsOnce()
    {
        // Instalação antiga: o monitor fica sempre listado e não há tarefas para escondê-lo.
        var monitor = new FakeVirtualMonitor { CanPowerOff = false };
        var missing = 0;
        var driverMissing = 0;
        var factory = new VideoFeedFactory("ffmpeg", WorkingRunner(), () => monitor.Attached ? new[] { Main, Virtual } : new[] { Main }, () => null, _ => { }, monitor);
        factory.DeviceSwitchMissing += () => missing++;
        factory.DriverMissing += () => driverMissing++;

        await using var feed = await factory.OpenAsync(Hello, CancellationToken.None);

        Assert.Equal(VideoKind.Extend, feed!.Kind);
        Assert.Equal(1, missing);
        Assert.Equal(0, driverMissing);
    }

    [Fact]
    public async Task TasksPresentDoNotWarnAboutTheSwitch()
    {
        var monitor = new FakeVirtualMonitor();
        var missing = 0;
        var factory = new VideoFeedFactory("ffmpeg", WorkingRunner(), () => monitor.Attached ? new[] { Main, Virtual } : new[] { Main }, () => null, _ => { }, monitor);
        factory.DeviceSwitchMissing += () => missing++;

        await using var feed = await factory.OpenAsync(Hello, CancellationToken.None);

        Assert.Equal(VideoKind.Extend, feed!.Kind);
        Assert.Equal(0, missing);
    }

    [Fact]
    public async Task CancellationAfterAttachDetachesAndPowersOffOnce()
    {
        var monitor = new FakeVirtualMonitor();
        using var cts = new CancellationTokenSource();
        // A saída nunca aparece na DXGI; o cancelamento chega durante a espera.
        var factory = new VideoFeedFactory("ffmpeg", WorkingRunner(), () =>
        {
            if (monitor.Attached)
            {
                cts.Cancel();
            }
            return new[] { Main, Side };
        }, () => null, _ => { }, monitor)
        {
            OutputWait = TimeSpan.FromSeconds(30),
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => factory.OpenAsync(Hello, cts.Token));

        Assert.Equal(new[] { "PowerOn", "Attach", "Detach", "PowerOff" }, monitor.Calls);
    }

    [Fact]
    public async Task PowerOffFailureAfterAFailedExtendIsLoggedAndMirrors()
    {
        var monitor = new FakeVirtualMonitor { PowerOffError = new IOException("o Agendador de Tarefas não respondeu") };
        var log = new ConcurrentQueue<string>();
        var factory = new VideoFeedFactory("ffmpeg", WorkingRunner(), () => [Main, Side], () => null, log.Enqueue, monitor)
        {
            OutputWait = TimeSpan.FromMilliseconds(200),
        };

        await using var feed = await factory.OpenAsync(Hello, CancellationToken.None);

        Assert.Equal(VideoKind.Mirror, feed!.Kind);
        Assert.Contains("não deu para desativar o monitor virtual: o Agendador de Tarefas não respondeu", log);
        Assert.Equal(1, monitor.PowerOffs);
    }

    [Fact]
    public async Task WithoutTheDriverMirrorsAndWarns()
    {
        var monitor = FakeVirtualMonitor.Absent();
        var warned = 0;
        var factory = new VideoFeedFactory("ffmpeg", WorkingRunner(), () => [Main, Side], () => null, _ => { }, monitor);
        factory.DriverMissing += () => warned++;

        await using var feed = await factory.OpenAsync(Hello, CancellationToken.None);

        Assert.Equal(VideoKind.Mirror, feed!.Kind);
        Assert.Equal(new VideoConfig(1366, 768, 60), feed.Config);
        Assert.Equal(1, warned);
        Assert.Empty(monitor.Detached);
        Assert.Equal(1, monitor.PowerOns);
        Assert.Equal(0, monitor.PowerOffs);
    }

    [Fact]
    public async Task AttachFailureFallsBackToMirror()
    {
        var monitor = new FakeVirtualMonitor { AttachError = new DisplayChangeException(-2, "ligar") };
        var log = new ConcurrentQueue<string>();
        var factory = new VideoFeedFactory("ffmpeg", WorkingRunner(), () => [Main, Side], () => null, log.Enqueue, monitor);

        await using var feed = await factory.OpenAsync(Hello, CancellationToken.None);

        Assert.Equal(VideoKind.Mirror, feed!.Kind);
        Assert.Contains(log, line => line.StartsWith("não deu para ligar o monitor virtual", StringComparison.Ordinal));
        Assert.Equal(new[] { "PowerOn", "PowerOff" }, monitor.Calls);
    }

    [Fact]
    public async Task VirtualOutputMissingFromDxgiDetachesAndMirrors()
    {
        var monitor = new FakeVirtualMonitor();
        var log = new ConcurrentQueue<string>();
        var factory = new VideoFeedFactory("ffmpeg", WorkingRunner(), () => [Main, Side], () => null, log.Enqueue, monitor)
        {
            OutputWait = TimeSpan.FromMilliseconds(200),
        };

        await using var feed = await factory.OpenAsync(Hello, CancellationToken.None);

        Assert.Equal(VideoKind.Mirror, feed!.Kind);
        Assert.Equal(new[] { "PowerOn", "Attach", "Detach", "PowerOff" }, monitor.Calls);
        Assert.Contains(log, line => line.Contains("fora do adaptador 0", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MirrorNeverPicksTheVirtualMonitorThatIsStillOn()
    {
        // O driver não respondeu ao PING, mas o monitor dele continua no desktop (ex.: o Detach anterior falhou).
        var monitor = FakeVirtualMonitor.Absent();
        monitor.Attached = true;
        var factory = new VideoFeedFactory("ffmpeg", WorkingRunner(), () => [Main, Virtual, Side], () => null, _ => { }, monitor);

        await using var feed = await factory.OpenAsync(Hello, CancellationToken.None);

        Assert.Equal(VideoKind.Mirror, feed!.Kind);
        Assert.Equal(new VideoConfig(1366, 768, 60), feed.Config);
    }

    [Fact]
    public async Task AttachThatDoesNotReachTheDesktopSaysSoAndMirrors()
    {
        var monitor = new FakeVirtualMonitor { AttachTakes = false };
        var log = new ConcurrentQueue<string>();
        var factory = new VideoFeedFactory("ffmpeg", WorkingRunner(), () => [Main, Side], () => null, log.Enqueue, monitor)
        {
            OutputWait = TimeSpan.FromMilliseconds(200),
        };

        await using var feed = await factory.OpenAsync(Hello, CancellationToken.None);

        Assert.Equal(VideoKind.Mirror, feed!.Kind);
        Assert.Contains(log, line => line.Contains("não entrou no desktop", StringComparison.Ordinal) && line.Contains("gravando no registro", StringComparison.Ordinal));
        Assert.DoesNotContain(log, line => line.Contains("fora do adaptador 0", StringComparison.Ordinal));
    }

    [Fact]
    public async Task WaitsForAVirtualOutputThatAppearsLateAndLogsHowLong()
    {
        var monitor = new FakeVirtualMonitor();
        var log = new ConcurrentQueue<string>();
        var since = System.Diagnostics.Stopwatch.StartNew();
        // A saída do monitor virtual só aparece na DXGI 300 ms depois de ligar.
        var factory = new VideoFeedFactory("ffmpeg", WorkingRunner(),
            () => monitor.Attached && since.ElapsedMilliseconds >= 300 ? new[] { Main, Virtual } : new[] { Main }, () => null, log.Enqueue, monitor);

        await using var feed = await factory.OpenAsync(Hello, CancellationToken.None);

        Assert.Equal(VideoKind.Extend, feed!.Kind);
        Assert.Contains(log, line => line.StartsWith($"monitor virtual {Virtual.DeviceName} na DXGI em ", StringComparison.Ordinal) && line.EndsWith(" ms (saída 2)", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FindsTheVirtualOutputIgnoringCase()
    {
        var monitor = new FakeVirtualMonitor();
        var lower = Virtual with { DeviceName = Virtual.DeviceName.ToLowerInvariant() };
        var factory = new VideoFeedFactory("ffmpeg", WorkingRunner(), () => monitor.Attached ? new[] { Main, lower } : new[] { Main }, () => null, _ => { }, monitor)
        {
            OutputWait = TimeSpan.FromMilliseconds(500),
        };

        await using var feed = await factory.OpenAsync(Hello, CancellationToken.None);

        Assert.Equal(VideoKind.Extend, feed!.Kind);
    }

    [Fact]
    public void OutputWaitDefaultsToFiveSeconds() =>
        Assert.Equal(TimeSpan.FromSeconds(5), new VideoFeedFactory("ffmpeg", WorkingRunner(), () => [], () => null, _ => { }).OutputWait);

    [Fact]
    public async Task LogsWhenTheDriverHasNoExactMode()
    {
        var monitor = new FakeVirtualMonitor { Mode = new DisplayModeInfo(2622, 1206, 60) };
        var wide = Virtual with { Width = 2622, Height = 1206 };
        var log = new ConcurrentQueue<string>();
        var factory = new VideoFeedFactory("ffmpeg", WorkingRunner(), () => monitor.Attached ? new[] { Main, wide } : new[] { Main }, () => null, log.Enqueue, monitor);

        await using var feed = await factory.OpenAsync(Hello, CancellationToken.None);

        Assert.Equal(new VideoConfig(2622, 1206, 60), feed!.Config);
        // Driver sem os modos novos (instalador antigo): avisa no registro para rodar o instalador de novo.
        Assert.Contains(log, line => line.StartsWith("o monitor virtual não tem 1460×674: usando 2622×1206", StringComparison.Ordinal)
            && line.Contains("instalar-vdd.ps1", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExtendsAtTheIphoneResolutionDividedByOnePointSevenFive()
    {
        // Etapa 5d (§7): em 100%, 1460×674 mostra o texto do tamanho de 2556×1179 em 175%, sem misturar escalas.
        var monitor = new FakeVirtualMonitor { Mode = new DisplayModeInfo(1460, 674, 60) };
        var small = Virtual with { Width = 1460, Height = 674 };
        var log = new ConcurrentQueue<string>();
        var factory = new VideoFeedFactory("ffmpeg", WorkingRunner(), () => monitor.Attached ? new[] { Main, small } : new[] { Main }, () => null, log.Enqueue, monitor);

        await using var feed = await factory.OpenAsync(Hello, CancellationToken.None);

        Assert.Equal((1460, 674), monitor.AttachRequested);
        Assert.Equal(VideoKind.Extend, feed!.Kind);
        Assert.Equal(new VideoConfig(1460, 674, 60), feed.Config);
        Assert.DoesNotContain(log, line => line.Contains("não tem"));
    }

    [Fact]
    public async Task ProbeFailureDetachesTheVirtualMonitor()
    {
        var monitor = new FakeVirtualMonitor();
        var factory = new VideoFeedFactory("ffmpeg", new FakeProcessRunner(_ => new FakeProcess(1)),
            () => monitor.Attached ? new[] { Main, Virtual } : new[] { Main }, () => null, _ => { }, monitor);

        Assert.Null(await factory.OpenAsync(Hello, CancellationToken.None));
        Assert.Equal(new[] { "PowerOn", "Attach", "Detach", "PowerOff" }, monitor.Calls);
    }

    /// <summary>Sondagens iniciadas até agora (a captura pode estar iniciando o FFmpeg ao mesmo tempo: lê sob a trava).</summary>
    private static int ProbeCount(FakeProcessRunner runner)
    {
        lock (runner.Started)
        {
            return runner.Started.Count(a => a.Contains("-frames:v"));
        }
    }

    /// <summary>Sondagem: só o h264_mf funciona; captura: o FFmpeg fica rodando sem entregar nenhuma imagem.</summary>
    private static (FakeProcessRunner Runner, VideoFeedFactory Factory, ConcurrentQueue<string> Log) HangingCapture(bool extend)
    {
        var runner = new FakeProcessRunner(args => args.Contains("-frames:v") ? new FakeProcess(args.Contains("h264_mf") ? 0 : 1) : new FakeProcess(0, hangs: true, stream: new BlockingStream()));
        var monitor = extend ? new FakeVirtualMonitor() : FakeVirtualMonitor.Absent();
        var log = new ConcurrentQueue<string>();
        var factory = new VideoFeedFactory("ffmpeg", runner, () => monitor.Attached ? new[] { Main, Virtual } : new[] { Main, Side }, () => null, log.Enqueue, monitor);
        return (runner, factory, log);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CaptureThatExitsWithoutAFrameMakesTheNextOpenProbeAgain(bool extend)
    {
        // Sondagem: só o h264_mf funciona; captura: o FFmpeg sai sem imagem (a linha real falha nesta saída).
        var runner = new FakeProcessRunner(args => args.Contains("-frames:v") ? new FakeProcess(args.Contains("h264_mf") ? 0 : 1) : new FakeProcess(1));
        var monitor = extend ? new FakeVirtualMonitor() : FakeVirtualMonitor.Absent();
        var log = new ConcurrentQueue<string>();
        var factory = new VideoFeedFactory("ffmpeg", runner, () => monitor.Attached ? new[] { Main, Virtual } : new[] { Main, Side }, () => null, log.Enqueue, monitor);
        int Probes() => ProbeCount(runner);

        var first = await factory.OpenAsync(Hello, CancellationToken.None);
        Assert.Equal(extend ? VideoKind.Extend : VideoKind.Mirror, first!.Kind);
        var probesBefore = Probes();
        // O FFmpeg sai sozinho (sem ninguém desligar): a captura avisa e a compressão é esquecida.
        await WaitForLogAsync(log, "a captura com mf terminou sem nenhuma imagem: a próxima conexão testa as compressões de novo");
        await first.DisposeAsync();

        await using var second = await factory.OpenAsync(Hello, CancellationToken.None);
        Assert.NotNull(second);
        Assert.Equal(2 * probesBefore, Probes());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FirstFrameDeadlineMakesTheNextOpenProbeAgain(bool extend)
    {
        var (runner, factory, log) = HangingCapture(extend);
        var first = await factory.OpenAsync(Hello, CancellationToken.None);
        var probesBefore = ProbeCount(runner);
        first!.FirstFrameMissed(); // o prazo da 1ª imagem estourou (a sessão avisa o feed)
        await first.DisposeAsync();
        Assert.Contains("a captura com mf terminou sem nenhuma imagem: a próxima conexão testa as compressões de novo", log);

        await using var second = await factory.OpenAsync(Hello, CancellationToken.None);
        Assert.NotNull(second);
        Assert.Equal(2 * probesBefore, ProbeCount(runner));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SwitchingOffBeforeTheFirstFrameKeepsTheProfile(bool extend)
    {
        var (runner, factory, log) = HangingCapture(extend);
        var first = await factory.OpenAsync(Hello, CancellationToken.None);
        var probesBefore = ProbeCount(runner);
        await first!.DisposeAsync(); // o usuário desligou (ou tirou o cabo) antes da 1ª imagem
        Assert.DoesNotContain(log, line => line.Contains("testa as compressões de novo"));

        await using var second = await factory.OpenAsync(Hello, CancellationToken.None);
        Assert.NotNull(second);
        Assert.Equal(probesBefore, ProbeCount(runner));
    }

    private static async Task WaitForLogAsync(ConcurrentQueue<string> log, string line)
    {
        var until = Environment.TickCount64 + 5000;
        while (!log.Contains(line))
        {
            Assert.True(Environment.TickCount64 < until, $"o registro não recebeu: {line}");
            await Task.Delay(10);
        }
    }

    [Fact]
    public async Task CaptureThatDeliversKeepsTheProfile()
    {
        var runner = WorkingRunner();
        var factory = new VideoFeedFactory("ffmpeg", runner, () => [Main, Side], () => null, _ => { });
        int Probes() => ProbeCount(runner);

        var first = await factory.OpenAsync(Hello, CancellationToken.None);
        await first!.NextAsync(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
        var probesBefore = Probes();
        await first.DisposeAsync();

        await using var second = await factory.OpenAsync(Hello, CancellationToken.None);
        Assert.Equal(probesBefore, Probes());
    }

    [Fact]
    public async Task LogsWhenTheMonitorStaysAfterAFailedOpen()
    {
        var monitor = new FakeVirtualMonitor { StaysAttached = true };
        var log = new ConcurrentQueue<string>();
        var factory = new VideoFeedFactory("ffmpeg", WorkingRunner(), () => [Main, Side], () => null, log.Enqueue, monitor)
        {
            OutputWait = TimeSpan.FromMilliseconds(200),
        };

        await using var feed = await factory.OpenAsync(Hello, CancellationToken.None);

        Assert.Equal(VideoKind.Mirror, feed!.Kind);
        Assert.Contains($"o monitor virtual {monitor.Device} continua no desktop depois de desligar", log);
    }
}
