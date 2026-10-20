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
        var display = new VirtualDisplay(Win32DisplayApi.ForVirtualMonitor(), new DriverPipe()); // persist: true, como a bandeja
        if (!await display.IsInstalledAsync(CancellationToken.None))
        {
            output.WriteLine("PULADO: o Virtual Display Driver não está instalado nesta máquina.");
            return;
        }
        var device = display.Find();
        Assert.NotNull(device); // o nome do adaptador confere com VirtualDisplay.AdapterName
        try
        {
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
        }
        Assert.False(display.IsAttached(device!));
    }
}
