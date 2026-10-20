using TelaDois.Core;
using TelaDois.Core.Display;
using TelaDois.Core.Link;
using TelaDois.Core.Usbmux;
using TelaDois.Core.Video;

namespace TelaDois.Tray;

/// <summary>Ícone na bandeja: estado do túnel, Modo monitor (Win+Shift+]) e o monitor duplicado sem driver.</summary>
internal sealed class TrayContext : ApplicationContext
{
    /// <summary>
    /// Ligar o monitor virtual gravando no registro do Windows. Falso até a validação no driver real (Task 2, Step 11
    /// da Etapa 5b) mostrar que o Windows exige gravar.
    /// </summary>
    private const bool PersistVirtualMonitor = false;

    private readonly NotifyIcon _icon;
    private readonly FileLog _log = FileLog.Default();
    private readonly CancellationTokenSource _cts = new();
    private readonly TraySettings _settings = TraySettings.Load();
    private readonly MonitorMode _monitor = new();
    private readonly VirtualDisplay _virtualDisplay = new(new Win32DisplayApi(PersistVirtualMonitor), new DriverPipe());
    private readonly ToolStripMenuItem _monitorItem;
    private readonly SynchronizationContext _ui;
    private readonly Hotkey _hotkey;
    private readonly Task _running;
    private volatile bool _closing;
    private TunnelState? _lastState;
    private int _ffmpegWarned;
    private int _driverWarned;

    public TrayContext()
    {
        var menu = new ContextMenuStrip();
        _monitorItem = new ToolStripMenuItem("Modo monitor (Win+Shift+])") { Enabled = false };
        _monitorItem.Click += (_, _) => ToggleMonitor();
        menu.Items.Add(_monitorItem);
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
        _log.Write("TelaDois iniciado");
        DetachLeftover("ao abrir");

        _ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        var video = new VideoFeedFactory(FfmpegLocator.Find(), new SystemProcessRunner(), DisplayCatalog.Enumerate, () => _settings.Monitor, _log.Write, _virtualDisplay);
        video.FfmpegMissing += () =>
        {
            // Uma vez por execução: a fábrica avisa a cada conexão, o balão não precisa repetir.
            if (Interlocked.Exchange(ref _ffmpegWarned, 1) == 0)
            {
                PostToUi(() => _icon.ShowBalloonTip(10000, "TelaDois",
                    "Para a Segunda Tela, instale o FFmpeg (winget install Gyan.FFmpeg) e reinicie o TelaDois", ToolTipIcon.Warning));
            }
        };
        video.DriverMissing += () =>
        {
            // Também uma vez por execução: sem o driver, toda abertura do vídeo avisa.
            if (Interlocked.Exchange(ref _driverWarned, 1) == 0)
            {
                PostToUi(() => _icon.ShowBalloonTip(10000, "TelaDois",
                    @"Monitor virtual não instalado: duplicando um monitor. Para estender, rode tools\vdd\instalar-vdd.ps1 como administrador.", ToolTipIcon.Info));
            }
        };
        // Os eventos chegam de threads diferentes e fora da trava (podem chegar fora de ordem): na thread da bandeja,
        // relê o estado atual em vez de confiar nos argumentos.
        _monitor.StateChanged += (_, _) => PostToUi(() =>
        {
            _monitorItem.Enabled = _monitor.IsConnected;
            _monitorItem.Checked = _monitor.WantsVideo;
        });

        _hotkey = new Hotkey(']', ToggleMonitor);
        if (!_hotkey.Registered)
        {
            _log.Write("atalho Win+Shift+] indisponível (outro programa já usa ou a tecla não existe neste teclado)");
            _icon.ShowBalloonTip(10000, "TelaDois", "O atalho Win+Shift+] está em uso por outro programa. Use o item \"Modo monitor\" da bandeja.", ToolTipIcon.Warning);
        }

        var service = new UsbTunnelService(new UsbmuxClient(UsbmuxClient.DefaultEndpoint), LinkSessionOptions.Default, _log.Write, video.OpenAsync, _monitor);
        service.StatusChanged += status => PostToUi(() => Show(status));
        _running = Task.Run(() => service.RunAsync(_cts.Token));
    }

    /// <summary>
    /// Leva <paramref name="action"/> para a thread da bandeja. Nunca lança: quem chama é o túnel (ou o
    /// <see cref="MonitorMode"/> dentro dele), e uma exceção aqui derrubaria a sessão. Depois de fechar, não faz nada.
    /// </summary>
    private void PostToUi(Action action)
    {
        if (_closing)
        {
            return;
        }
        try
        {
            _ui.Post(_ =>
            {
                if (_closing)
                {
                    return;
                }
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    _log.Write($"falha ao atualizar a bandeja: {e.Message}");
                }
            }, null);
        }
        catch (Exception e)
        {
            // A janela da bandeja já foi destruída (saindo): não há o que atualizar.
            _log.Write($"bandeja indisponível: {e.Message}");
        }
    }

    private void ToggleMonitor()
    {
        var result = _monitor.Toggle();
        _log.Write(result switch
        {
            ToggleResult.TurnedOn => "Modo monitor: ligado",
            ToggleResult.TurnedOff => "Modo monitor: desligado",
            _ => "Modo monitor: nenhum iPhone conectado",
        });
        if (result == ToggleResult.NoPhone)
        {
            _icon.ShowBalloonTip(3000, "TelaDois", "Conecte o iPhone pelo cabo com o TelaDois aberto", ToolTipIcon.Info);
        }
    }

    /// <summary>
    /// Tira do desktop o monitor virtual que ficou ligado (queda anterior, recém-instalado ou a sessão que não terminou
    /// a tempo ao sair). O <see cref="VirtualDisplay.Detach"/> não avisa se o Windows ignorou: confere e registra.
    /// </summary>
    private void DetachLeftover(string when)
    {
        try
        {
            if (_virtualDisplay.DetachLeftover() is { } leftover)
            {
                _log.Write(_virtualDisplay.IsAttached(leftover)
                    ? $"{when}: o monitor virtual {leftover} continua no desktop depois de desligar"
                    : $"{when}: monitor virtual que sobrou ligado foi desligado ({leftover})");
            }
        }
        catch (Exception e) when (e is DisplayChangeException or InvalidOperationException)
        {
            _log.Write($"{when}: não deu para desligar o monitor virtual que sobrou: {e.Message}");
        }
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
        // Ao sair, a sessão descarta o vídeo e o monitor virtual sai do desktop: espera um pouco por isso.
        _closing = true;
        _cts.Cancel();
        try
        {
            _running.Wait(TimeSpan.FromSeconds(3));
        }
        catch (AggregateException)
        {
        }
        DetachLeftover("ao sair");
        _hotkey.Dispose();
        _icon.Visible = false;
        _icon.Dispose();
        _log.Write("TelaDois encerrado");
        base.ExitThreadCore();
    }
}
