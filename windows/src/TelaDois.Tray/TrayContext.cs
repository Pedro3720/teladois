using TelaDois.Core;
using TelaDois.Core.Link;
using TelaDois.Core.Usbmux;

namespace TelaDois.Tray;

/// <summary>Ícone na bandeja que mostra o estado do túnel com o iPhone.</summary>
internal sealed class TrayContext : ApplicationContext
{
    private readonly NotifyIcon _icon;
    private readonly FileLog _log = FileLog.Default();
    private readonly CancellationTokenSource _cts = new();
    private TunnelState? _lastState;

    public TrayContext()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Abrir registro", null, (_, _) => _log.Open());
        menu.Items.Add("Sair", null, (_, _) => ExitThread());
        _icon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "TelaDois: iniciando",
            ContextMenuStrip = menu,
            Visible = true,
        };

        var ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        var service = new UsbTunnelService(new UsbmuxClient(UsbmuxClient.DefaultEndpoint), LinkSessionOptions.Default, _log.Write);
        service.StatusChanged += status => ui.Post(_ => Show(status), null);
        _log.Write("TelaDois iniciado");
        _ = Task.Run(() => service.RunAsync(_cts.Token));
    }

    private void Show(TunnelStatus status)
    {
        var text = TunnelStatusText.For(status);
        _icon.Text = text;
        if (status.State == _lastState)
        {
            return;
        }
        _lastState = status.State;
        _log.Write(text);
        if (status.State == TunnelState.Connected)
        {
            _icon.ShowBalloonTip(3000, "TelaDois", "iPhone conectado pelo cabo", ToolTipIcon.Info);
        }
        else if (status.State == TunnelState.ServiceMissing)
        {
            _icon.ShowBalloonTip(10000, "TelaDois",
                "Instale o iTunes baixado do site da Apple (não o da Microsoft Store) para o PC enxergar o iPhone.",
                ToolTipIcon.Warning);
        }
    }

    protected override void ExitThreadCore()
    {
        _cts.Cancel();
        _icon.Visible = false;
        _icon.Dispose();
        _log.Write("TelaDois encerrado");
        base.ExitThreadCore();
    }
}
