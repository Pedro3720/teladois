using TelaDois.Core.Display;
using TelaDois.Core.Link;

namespace TelaDois.Core.Video;

/// <summary>
/// Captura do monitor virtual (Estender). Confere a cada <c>checkInterval</c> se o monitor continua no desktop: se o
/// usuário o desligou nas Configurações, <see cref="NextAsync"/> termina com <see cref="OperationCanceledException"/>
/// (a sessão manda VIDEO_END). Descartar encerra a captura, tira o monitor do desktop e desativa o dispositivo do driver.
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

    public VideoMeters? Meters => _capture.Meters;

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

    /// <summary>
    /// Desativa o dispositivo do driver (o Windows deixa de listar o monitor). Nunca lança: falhas, até o cancelamento,
    /// só vão para o registro; não recebe o token da sessão porque também roda depois de cancelar.
    /// </summary>
    internal static async Task PowerOffQuietlyAsync(IVirtualMonitor monitor, Action<string> log)
    {
        try
        {
            await monitor.PowerOffAsync(CancellationToken.None);
        }
        catch (Exception e)
        {
            try
            {
                log($"não deu para desativar o monitor virtual: {e.Message}");
            }
            catch (Exception)
            {
            }
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

    /// <summary>
    /// Encerra a captura e, mesmo se isso falhar, tira o monitor do desktop (na dúvida, tenta tirar) e desativa o
    /// dispositivo (sempre: o usuário pode ter tirado o monitor do desktop, mas ele continua listado).
    /// </summary>
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
            await PowerOffQuietlyAsync(_monitor, _log);
            _gone.Dispose();
        }
    }
}
