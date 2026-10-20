using TelaDois.Core.Display;
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Display;

/// <summary>Com o driver real (pisca o monitor extra por ~1 s). Retorna cedo onde o driver não está instalado (CI).</summary>
[Trait("Category", "Local")]
public class VirtualDisplayLocalTests
{
    [Fact]
    public async Task RealDriverMonitorAttachesShowsUpInDxgiAndDetaches()
    {
        var display = new VirtualDisplay(new Win32DisplayApi(persist: false), new DriverPipe());
        if (!await display.IsInstalledAsync(CancellationToken.None))
        {
            return; // sem o Virtual Display Driver nesta máquina
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
