using TelaDois.Core.Display;
using TelaDois.Core.Link;

namespace TelaDois.Core.Video;

/// <summary>
/// Captura do monitor virtual (Estender). Confere a cada <c>checkInterval</c> se o monitor continua no desktop: se o
/// usuário o desligou nas Configurações, <see cref="NextAsync"/> termina com <see cref="OperationCanceledException"/>
/// (a sessão manda VIDEO_END). Descartar encerra a captura e tira o monitor do desktop.
/// </summary>
public sealed class VirtualMonitorFeed : IVideoFeed
{
    private readonly IVideoFeed _capture;
    private readonly IVirtualMonitor _monitor;
    private readonly string _deviceName;
    private readonly Action<string> _log;
    private readonly CancellationTokenSource _gone = new();
    private readonly Task _watch;
    private int _disposed;

    public VirtualMonitorFeed(IVideoFeed capture, IVirtualMonitor monitor, string deviceName, Action<string> log, TimeSpan? checkInterval = null)
    {
        _capture = capture;
        _monitor = monitor;
        _deviceName = deviceName;
        _log = log;
        _watch = WatchAsync(checkInterval ?? TimeSpan.FromSeconds(1));
    }

    public VideoConfig Config => _capture.Config;

    public VideoKind Kind => VideoKind.Extend;

    public async Task<AccessUnit> NextAsync(CancellationToken ct)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _gone.Token);
        return await _capture.NextAsync(linked.Token);
    }

    public void FirstFrameMissed() => _capture.FirstFrameMissed();

    /// <summary>
    /// Tira o monitor do desktop pelo <see cref="IVirtualMonitor"/> (que ignora qualquer monitor que não seja o virtual) e,
    /// como o desligamento é silencioso, confere se ele saiu mesmo; falhas só vão para o registro.
    /// </summary>
    internal static void DetachAndVerify(IVirtualMonitor monitor, string deviceName, Action<string> log)
    {
        try
        {
            monitor.Detach(deviceName);
            if (monitor.IsAttached(deviceName))
            {
                log($"o monitor virtual {deviceName} continua no desktop depois de desligar");
            }
        }
        catch (Exception e)
        {
            log($"não deu para desligar o monitor virtual: {e.Message}");
        }
    }

    /// <summary>Nunca lança: se a conferência falhar, registra e para de conferir (o vídeo segue).</summary>
    private async Task WatchAsync(TimeSpan interval)
    {
        try
        {
            while (true)
            {
                await Task.Delay(interval, _gone.Token);
                if (!_monitor.IsAttached(_deviceName))
                {
                    _log("o monitor virtual saiu do desktop: Segunda Tela desligada");
                    _gone.Cancel();
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            _log($"não deu para conferir o monitor virtual: {e.Message}");
        }
    }

    /// <summary>Encerra a captura e, mesmo se isso falhar, tira o monitor do desktop (na dúvida, tenta tirar).</summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }
        _gone.Cancel();
        try
        {
            await _watch;
            await _capture.DisposeAsync();
        }
        finally
        {
            bool attached;
            try
            {
                attached = _monitor.IsAttached(_deviceName);
            }
            catch (Exception e)
            {
                _log($"não deu para conferir o monitor virtual: {e.Message}");
                attached = true;
            }
            if (attached)
            {
                DetachAndVerify(_monitor, _deviceName, _log);
            }
            _gone.Dispose();
        }
    }
}
