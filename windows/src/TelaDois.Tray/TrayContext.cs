using TelaDois.Core;
using TelaDois.Core.Display;
using TelaDois.Core.Link;
using TelaDois.Core.Usbmux;
using TelaDois.Core.Video;

namespace TelaDois.Tray;

/// <summary>Ícone na bandeja: estado do túnel, Modo monitor (Win+Shift+]), tela preta (Win+Shift+[) e o monitor duplicado sem driver.</summary>
internal sealed class TrayContext : ApplicationContext
{
    /// <summary>Ao sair, quanto esperar a sessão descartar o vídeo e mandar BYE.</summary>
    private static readonly TimeSpan ExitWait = TimeSpan.FromSeconds(6);

    /// <summary>Ao sair, quanto esperar o dispositivo do driver ser desativado.</summary>
    private static readonly TimeSpan PowerOffExitWait = TimeSpan.FromSeconds(5);

    private readonly NotifyIcon _icon;
    private readonly FileLog _log = FileLog.Default();
    private readonly CancellationTokenSource _cts = new();
    private readonly TraySettings _settings = TraySettings.Load();
    private readonly MonitorMode _monitor = new();
    private readonly VirtualDisplay _virtualDisplay;
    private readonly ToolStripMenuItem _monitorItem;
    private readonly ToolStripMenuItem _darkItem;
    private readonly SynchronizationContext _ui;
    private readonly Hotkey _hotkey;
    private readonly Hotkey _darkHotkey;
    private readonly Task _running;
    private volatile bool _closing;
    private TunnelState? _lastState;
    private int _ffmpegWarned;
    private int _driverWarned;
    private int _switchWarned;
    private int _scaleWarned;

    public TrayContext()
    {
        _virtualDisplay = new VirtualDisplay(Win32DisplayApi.ForVirtualMonitor(), new DriverPipe(),
            new ScheduledTaskSwitch(new SystemProcessRunner(), _log.Write), _log.Write);
        var menu = new ContextMenuStrip();
        _monitorItem = new ToolStripMenuItem("Modo monitor (Win+Shift+])") { Enabled = false };
        _monitorItem.Click += (_, _) => ToggleMonitor();
        menu.Items.Add(_monitorItem);
        _darkItem = new ToolStripMenuItem("Tela preta no iPhone (Win+Shift+[)") { Enabled = false };
        _darkItem.Click += (_, _) => ToggleDark();
        menu.Items.Add(_darkItem);
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
        video.DeviceSwitchMissing += () =>
        {
            // Também uma vez por execução: a fábrica avisa a cada conexão.
            if (Interlocked.Exchange(ref _switchWarned, 1) == 0)
            {
                PostToUi(() => _icon.ShowBalloonTip(10000, "TelaDois",
                    "Rode o instalador do monitor virtual de novo (como administrador) para o Windows esconder o monitor quando ele não estiver em uso.", ToolTipIcon.Info));
            }
        };
        video.ScaleNotNative += percent =>
        {
            // Uma vez por execução: a fábrica avisa a cada vez que liga o monitor; o TelaDois não muda a escala sozinho.
            if (Interlocked.Exchange(ref _scaleWarned, 1) == 0)
            {
                PostToUi(() => _icon.ShowBalloonTip(15000, "TelaDois",
                    $"O monitor do iPhone está em {percent}%. Para o programa do trabalho funcionar em todas as telas, deixe o monitor do iPhone em 100%: " +
                    "Configurações → Tela → monitor do iPhone → Escala 100%", ToolTipIcon.Warning));
            }
        };
        // Os eventos chegam de threads diferentes e fora da trava (podem chegar fora de ordem): na thread da bandeja,
        // relê o estado atual em vez de confiar nos argumentos.
        _monitor.StateChanged += (_, _) => PostToUi(RefreshMenu);
        // DarkChanged chega na thread que lê o socket do iPhone (toque na tela): só posta, nunca bloqueia nem lança.
        _monitor.DarkChanged += _ => PostToUi(RefreshMenu);

        _hotkey = new Hotkey(']', ToggleMonitor);
        if (!_hotkey.Registered)
        {
            _log.Write("atalho Win+Shift+] indisponível (outro programa já usa ou a tecla não existe neste teclado)");
            _icon.ShowBalloonTip(10000, "TelaDois", "O atalho Win+Shift+] está em uso por outro programa. Use o item \"Modo monitor\" da bandeja.", ToolTipIcon.Warning);
        }

        _darkHotkey = new Hotkey('[', ToggleDark);
        if (!_darkHotkey.Registered)
        {
            _log.Write("atalho Win+Shift+[ não está disponível (outro programa já usa ou a tecla não existe neste teclado)");
            _icon.ShowBalloonTip(10000, "TelaDois", "O atalho Win+Shift+[ não está disponível. Use o item \"Tela preta no iPhone\" da bandeja.", ToolTipIcon.Warning);
        }

        var service = new UsbTunnelService(new UsbmuxClient(UsbmuxClient.DefaultEndpoint), LinkSessionOptions.Default, _log.Write, video.OpenAsync, _monitor);
        service.StatusChanged += status => PostToUi(() => Show(status));
        // O desligamento de abrir termina antes do túnel começar: senão ele desativaria o dispositivo no meio de uma
        // sessão (iPhone já plugado e monitor que sobrou ativo: o PowerOnAsync da sessão acharia o monitor e seguiria).
        _running = Task.Run(async () =>
        {
            await PowerOffOnOpenAsync();
            await service.RunAsync(_cts.Token);
        });
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

    /// <summary>Na thread da bandeja: relê o estado do <see cref="MonitorMode"/> e acerta os dois itens do menu.</summary>
    private void RefreshMenu()
    {
        _monitorItem.Enabled = _monitor.IsConnected;
        _monitorItem.Checked = _monitor.WantsVideo;
        _darkItem.Enabled = _monitor.IsConnected && _monitor.WantsVideo;
        _darkItem.Checked = _monitor.WantsDark;
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

    private void ToggleDark()
    {
        _log.Write(_monitor.ToggleDark() switch
        {
            DarkToggleResult.TurnedDark => "tela preta: ligada",
            DarkToggleResult.TurnedLit => "tela preta: desligada",
            _ => "tela preta: modo monitor desligado",
        });
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

    /// <summary>
    /// Desativa o dispositivo do driver que ficou ativo (queda anterior ou instalação recém-feita). Roda numa tarefa de
    /// fundo (a tarefa do Windows e a espera pelo monitor sumir levam segundos, não prende a thread da bandeja) e o túnel
    /// só começa depois dela. Nunca lança; o resultado vai ao registro.
    /// </summary>
    private async Task PowerOffOnOpenAsync()
    {
        try
        {
            // O próprio VirtualDisplay registra o resultado (desativou, não desativou, tarefa recusada).
            await _virtualDisplay.PowerOffAsync(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            _log.Write("ao abrir: desativar o monitor virtual foi interrompido (saindo)");
        }
        catch (Exception e)
        {
            _log.Write($"ao abrir: não deu para desativar o monitor virtual: {e.Message}");
        }
    }

    /// <summary>
    /// Ao sair, deixa o dispositivo do driver desativado (o Windows esconde o monitor). Limitado a
    /// <see cref="PowerOffExitWait"/> para não prender o encerramento. Não usa o <c>_cts</c>, já cancelado. Nunca lança.
    /// </summary>
    private void PowerOffOnExit()
    {
        try
        {
            using var limit = new CancellationTokenSource(PowerOffExitWait + TimeSpan.FromSeconds(1));
            var task = Task.Run(() => _virtualDisplay.PowerOffAsync(limit.Token));
            if (!task.Wait(PowerOffExitWait))
            {
                _log.Write($"ao sair: o monitor virtual não foi desativado em {PowerOffExitWait.TotalSeconds:0} s; o próximo início tenta de novo");
                limit.Cancel();
                // Ninguém vai esperar o resultado: observa a falha para não virar exceção não observada.
                _ = task.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
            }
        }
        catch (Exception e)
        {
            _log.Write($"ao sair: não deu para desativar o monitor virtual: {(e as AggregateException)?.GetBaseException().Message ?? e.Message}");
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
        // O monitor virtual (estendido agora) não entra na lista: duplicá-lo seria transmitir o próprio iPhone.
        string? virtualMonitor;
        try
        {
            virtualMonitor = _virtualDisplay.Find();
        }
        catch (Exception e)
        {
            _log.Write($"não deu para procurar o monitor virtual: {e.Message}");
            virtualMonitor = null;
        }
        var outputs = DisplayCatalog.Without(DisplayCatalog.Enumerate(), virtualMonitor);
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
        // Ao sair, a sessão descarta o vídeo e o monitor virtual sai do desktop: espera por isso. 6 s cobre o limite do
        // descarte dos feeds na sessão (5 s, com o FFmpeg tendo até 3 s para sair) mais o BYE (0,5 s).
        _closing = true;
        _cts.Cancel();
        try
        {
            if (!_running.Wait(ExitWait))
            {
                _log.Write($"a sessão não terminou em {ExitWait.TotalSeconds:0} s ao sair: desligando o monitor virtual daqui");
            }
        }
        catch (AggregateException)
        {
        }
        DetachLeftover("ao sair");
        PowerOffOnExit();
        _hotkey.Dispose();
        _darkHotkey.Dispose();
        _icon.Visible = false;
        _icon.Dispose();
        _log.Write("TelaDois encerrado");
        base.ExitThreadCore();
    }
}
