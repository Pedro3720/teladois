using TelaDois.Core;
using TelaDois.Core.Link;
using TelaDois.Core.Usbmux;
using TelaDois.Core.Video;

namespace TelaDois.Tray;

/// <summary>Ícone na bandeja que mostra o estado do túnel com o iPhone.</summary>
internal sealed class TrayContext : ApplicationContext
{
    private readonly NotifyIcon _icon;
    private readonly FileLog _log = FileLog.Default();
    private readonly CancellationTokenSource _cts = new();
    private readonly TraySettings _settings = TraySettings.Load();
    private TunnelState? _lastState;
    private int _ffmpegWarned;

    public TrayContext()
    {
        var menu = new ContextMenuStrip();
        var monitors = new ToolStripMenuItem("Monitor transmitido");
        monitors.DropDownItems.Add("(carregando)");
        monitors.DropDownOpening += (_, _) => FillMonitors(monitors);
        menu.Items.Add(monitors);
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
        var video = new VideoFeedFactory(FfmpegLocator.Find(), new SystemProcessRunner(), DisplayCatalog.Enumerate, () => _settings.Monitor, _log.Write);
        video.FfmpegMissing += () =>
        {
            // Uma vez por execução: a fábrica avisa a cada conexão, o balão não precisa repetir.
            if (Interlocked.Exchange(ref _ffmpegWarned, 1) == 0)
            {
                ui.Post(_ => _icon.ShowBalloonTip(10000, "TelaDois",
                    "Para a Segunda Tela, instale o FFmpeg (winget install Gyan.FFmpeg) e reinicie o TelaDois", ToolTipIcon.Warning), null);
            }
        };
        var service = new UsbTunnelService(new UsbmuxClient(UsbmuxClient.DefaultEndpoint), LinkSessionOptions.Default, _log.Write, video.OpenAsync);
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

    private void FillMonitors(ToolStripMenuItem menu)
    {
        menu.DropDownItems.Clear();
        var outputs = DisplayCatalog.Enumerate();
        var current = DisplayCatalog.Choose(outputs, _settings.Monitor);
        foreach (var output in outputs)
        {
            var label = $"{output.DeviceName.Replace(@"\\.\", "")} · {output.Width}×{output.Height}{(output.IsPrimary ? " (principal)" : "")}";
            var item = new ToolStripMenuItem(label) { Checked = output == current };
            item.Click += (_, _) =>
            {
                _settings.Monitor = output.DeviceName;
                try
                {
                    _settings.Save();
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    _log.Write($"não deu para salvar a escolha do monitor: {e.Message}");
                }
                _log.Write($"monitor transmitido: {output.DeviceName} (vale na próxima conexão)");
            };
            menu.DropDownItems.Add(item);
        }
        if (outputs.Count == 0)
        {
            menu.DropDownItems.Add(new ToolStripMenuItem("nenhum monitor encontrado") { Enabled = false });
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
