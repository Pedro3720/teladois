using TelaDois.Core.Display;
using TelaDois.Core.Video;
using Xunit.Abstractions;

namespace TelaDois.Core.Tests.Display;

/// <summary>
/// Com o driver real: pisca o monitor extra por ~1 s e, no fim, desliga o monitor do driver. Por isso <b>só roda quando
/// pedido</b>. Num <c>dotnet test</c> comum (e no CI) retorna na hora. Para rodar, num PC com o driver instalado:
/// <c>$env:TELADOIS_LOCAL_DRIVER_TEST='1'; dotnet test windows/TelaDois.sln --filter FullyQualifiedName~VirtualDisplayLocalTests</c>
/// (o xUnit 2.9.3 não tem <c>Assert.Skip</c>: o teste passa e avisa na saída que foi pulado).
/// </summary>
[Trait("Category", "Local")]
public class VirtualDisplayLocalTests(ITestOutputHelper output)
{
    [Fact]
    public async Task RealDriverMonitorAttachesShowsUpInDxgiAndDetaches()
    {
        if (!LocalDriverGate.IsEnabled())
        {
            output.WriteLine($"PULADO: teste com o driver real só roda com {LocalDriverGate.Variable}=1 (ele liga e desliga um monitor de verdade).");
            return;
        }
        // Como a bandeja (Etapa 5c): o dispositivo fica desativado e só é ativado enquanto estende.
        var display = new VirtualDisplay(Win32DisplayApi.ForVirtualMonitor(), new DriverPipe(),
            new ScheduledTaskSwitch(new SystemProcessRunner(), output.WriteLine), output.WriteLine);
        var canPowerOff = await display.CanPowerOffAsync(CancellationToken.None);
        if (!await display.PowerOnAsync(CancellationToken.None))
        {
            Assert.False(canPowerOff, "as tarefas existem, mas o monitor virtual não ativou");
            output.WriteLine("PULADO: o Virtual Display Driver não está instalado nesta máquina.");
            return;
        }
        string? device = null;
        try
        {
            device = display.Find();
            Assert.NotNull(device); // o nome do adaptador confere com VirtualDisplay.AdapterName
            var (name, mode) = display.Attach(2556, 1179);
            Assert.Equal(new DisplayModeInfo(2556, 1179, 60), mode);
            Assert.True(display.IsAttached(name));

            // A DXGI demora um pouco a mostrar a saída nova.
            DisplayOutput? output = null;
            for (var i = 0; i < 20 && output is null; i++)
            {
                output = DisplayCatalog.Enumerate().FirstOrDefault(o => o.DeviceName == name);
                if (output is null)
                {
                    await Task.Delay(100);
                }
            }
            Assert.NotNull(output); // está no adaptador 0, o único que o ddagrab enxerga
            Assert.Equal((2556, 1179), (output!.Width, output.Height));

            if (FfmpegLocator.Find() is { } ffmpeg)
            {
                var x264 = EncoderProfiles.Candidates.Single(p => p.Id == "x264");
                await using var probe = new SystemProcessRunner().Start(ffmpeg, FfmpegArguments.Probe(x264, output.OutputIndex));
                Assert.Equal(0, await probe.WaitForExitAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(15)));
            }
        }
        finally
        {
            if (display.Find() is { } attached && display.IsAttached(attached))
            {
                display.Detach(attached);
            }
            await display.PowerOffAsync(CancellationToken.None);
        }
        Assert.False(display.IsAttached(device!));
        if (canPowerOff)
        {
            Assert.Null(display.Find()); // desativado, o Windows deixa de listar o monitor
        }
    }

    /// <summary>
    /// Desativa e ativa o dispositivo do driver pelas tarefas do Windows (criadas pelo instalador) e mede os tempos; termina
    /// com o dispositivo desativado, como o instalador o deixa.
    /// </summary>
    [Fact]
    public async Task RealDriverDevicePowersOffAndOnThroughTheScheduledTasks()
    {
        if (!LocalDriverGate.IsEnabled())
        {
            output.WriteLine($"PULADO: teste com o driver real só roda com {LocalDriverGate.Variable}=1 (ele desativa e ativa o dispositivo de verdade).");
            return;
        }
        var tasks = new ScheduledTaskSwitch(new SystemProcessRunner(), output.WriteLine);
        if (!await tasks.IsAvailableAsync(CancellationToken.None))
        {
            output.WriteLine(@"PULADO: as tarefas \TelaDois\ não existem nesta máquina (rode o tools\vdd\instalar-vdd.ps1 como administrador).");
            return;
        }
        var display = new VirtualDisplay(Win32DisplayApi.ForVirtualMonitor(), new DriverPipe(), tasks, output.WriteLine);
        Assert.True(await display.CanPowerOffAsync(CancellationToken.None));
        try
        {
            await display.PowerOffAsync(CancellationToken.None);
            Assert.Null(display.Find());

            var watch = System.Diagnostics.Stopwatch.StartNew();
            Assert.True(await display.PowerOnAsync(CancellationToken.None));
            output.WriteLine($"ligar: {watch.ElapsedMilliseconds} ms até o monitor aparecer ({display.Find()})");
            Assert.NotNull(display.Find());
        }
        finally
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            await display.PowerOffAsync(CancellationToken.None);
            output.WriteLine($"desligar: {watch.ElapsedMilliseconds} ms até o monitor sumir");
        }
        Assert.Null(display.Find());
    }
}
