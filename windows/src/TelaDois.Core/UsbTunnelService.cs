using System.Net.Sockets;
using TelaDois.Core.Link;
using TelaDois.Core.Usbmux;
using TelaDois.Core.Video;

namespace TelaDois.Core;

public enum TunnelState
{
    ServiceMissing,
    WaitingForIPhone,
    WaitingForApp,
    Connected,
}

public sealed record TunnelStatus(TunnelState State, HelloInfo? Hello = null, TimeSpan? RoundTrip = null, VideoConfig? Video = null);

/// <summary>
/// Liga tudo: escuta o usbmuxd; com um iPhone no cabo, tenta abrir o túnel até a porta 7100 a cada
/// <see cref="Retry"/> (recusado = app fechado) e conduz a sessão; ao desplugar, volta a esperar.
/// </summary>
public sealed class UsbTunnelService(UsbmuxClient usbmux, LinkSessionOptions options, Action<string> log, Func<HelloInfo, CancellationToken, Task<IVideoFeed?>>? openVideo = null)
{
    private string? _lastError;

    public event Action<TunnelStatus>? StatusChanged;

    public TimeSpan Retry { get; init; } = TimeSpan.FromSeconds(2);

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await WatchDevicesAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (SocketException e)
            {
                LogOnce($"usbmuxd indisponível: {e.Message}");
                Report(new TunnelStatus(TunnelState.ServiceMissing));
            }
            catch (Exception e) when (e is IOException or InvalidDataException)
            {
                LogOnce($"conexão com o usbmuxd caiu: {e.Message}");
            }
            try
            {
                await Task.Delay(Retry, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task WatchDevicesAsync(CancellationToken ct)
    {
        (int Id, CancellationTokenSource Cts, Task Task)? active = null;
        try
        {
            await foreach (var usbmuxEvent in usbmux.ListenAsync(ct))
            {
                switch (usbmuxEvent)
                {
                    case ListenStarted:
                        if (_lastError is not null)
                        {
                            log("usbmuxd disponível");
                            _lastError = null;
                        }
                        Report(new TunnelStatus(TunnelState.WaitingForIPhone));
                        break;
                    case DeviceAttached attached when attached.ConnectionType == "USB" && active is null:
                        log($"iPhone no cabo (DeviceID {attached.DeviceId}, {attached.SerialNumber})");
                        var deviceCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        active = (attached.DeviceId, deviceCts, RunDeviceAsync(attached.DeviceId, deviceCts.Token));
                        break;
                    case DeviceDetached detached when active?.Id == detached.DeviceId:
                        log($"iPhone saiu do cabo (DeviceID {detached.DeviceId})");
                        await StopAsync(active.Value);
                        active = null;
                        Report(new TunnelStatus(TunnelState.WaitingForIPhone));
                        break;
                }
            }
        }
        finally
        {
            if (active is not null)
            {
                await StopAsync(active.Value);
            }
        }
    }

    private async Task RunDeviceAsync(int deviceId, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            Report(new TunnelStatus(TunnelState.WaitingForApp));
            try
            {
                await using var tunnel = await usbmux.ConnectAsync(deviceId, LinkMessages.Port, ct);
                log("túnel aberto até o app do iPhone");
                var reason = await LinkSession.RunAsync(
                    tunnel,
                    status => Report(new TunnelStatus(TunnelState.Connected, status.Hello, status.RoundTrip, status.Video)),
                    options,
                    ct,
                    openVideo);
                log($"sessão encerrada: {reason}");
            }
            catch (UsbmuxException e) when (e.Code == UsbmuxMessages.ResultConnectionRefused)
            {
                // App fechado ou em segundo plano: tenta de novo.
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception e) when (e is IOException or InvalidDataException)
            {
                log($"falha no túnel: {e.Message}");
            }
            try
            {
                await Task.Delay(Retry, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private static async Task StopAsync((int Id, CancellationTokenSource Cts, Task Task) device)
    {
        device.Cts.Cancel();
        try
        {
            await device.Task;
        }
        catch (OperationCanceledException)
        {
        }
        device.Cts.Dispose();
    }

    private void LogOnce(string message)
    {
        if (message != _lastError)
        {
            log(message);
            _lastError = message;
        }
    }

    private void Report(TunnelStatus status) => StatusChanged?.Invoke(status);
}
