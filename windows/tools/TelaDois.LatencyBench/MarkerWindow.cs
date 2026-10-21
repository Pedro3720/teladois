using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TelaDois.LatencyBench;

/// <summary>
/// Janela da bancada: sem borda, sempre por cima, fora da barra de tarefas, sem roubar o foco, no tamanho exato em
/// pixels físicos (o processo é PerMonitorV2; a troca de DPI não redimensiona) e pintada só no <c>OnPaint</c>.
/// </summary>
internal class BenchForm : Form
{
    private const int WmDpiChanged = 0x02E0;
    private const int WsExNoActivate = 0x08000000;
    private const int WsExToolWindow = 0x00000080;

    private readonly Rectangle _target;

    protected BenchForm(Rectangle target)
    {
        _target = target;
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Bounds = target;
    }

    private static readonly IntPtr HwndTopmost = new(-1);
    private const uint SwpNoSize = 0x0001, SwpNoMove = 0x0002, SwpNoActivate = 0x0010, SwpShowWindow = 0x0040;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

    /// <summary>Volta para o topo da faixa "sempre por cima" sem pegar o foco (outro programa pode ter subido por cima).</summary>
    public void Raise() => SetWindowPos(Handle, HwndTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate | SwpShowWindow);

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WsExNoActivate | WsExToolWindow;
            return cp;
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Bounds = _target;
        // Com ShowWithoutActivation o WinForms não aplica o TopMost ao mostrar (medido: "SEM TOPMOST" até o 1º Raise).
        Raise();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmDpiChanged)
        {
            // Mantém o tamanho em pixels físicos: o recorte do marcador depende dele.
            m.Result = IntPtr.Zero;
            return;
        }
        base.WndProc(ref m);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
    }
}

/// <summary>O marcador de 64×64: o código atual em 4×4 blocos de 16 px (<see cref="MarkerCode"/>).</summary>
internal sealed class MarkerWindow(Rectangle bounds) : BenchForm(bounds)
{
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int Code { get; set; }

    protected override void OnPaint(PaintEventArgs e)
    {
        const int block = MarkerCode.Size / MarkerCode.Grid;
        var blocks = MarkerCode.Encode(Code);
        for (var i = 0; i < blocks.Length; i++)
        {
            e.Graphics.FillRectangle(blocks[i] ? Brushes.White : Brushes.Black,
                i % MarkerCode.Grid * block, i / MarkerCode.Grid * block, block, block);
        }
    }
}


/// <summary>
/// O que a bancada mostra no monitor medido: o marcador e a cena do cenário (<see cref="BenchLayout"/>). Uma thread de
/// interface (STA) mantém as janelas; uma thread de fundo troca o marcador:
/// <list type="bullet">
/// <item><c>rolagem</c>/<c>video</c>: uma volta por composição do DWM — <c>DwmFlush()</c> → anota o desenho anterior
/// (já composto) → troca o código, anda a cena e pinta.</item>
/// <item><c>parado</c>: nada é repintado entre as trocas (a tela fica parada de verdade); a cada 2 s pelo
/// <c>Stopwatch</c>: troca o código, pinta, <c>DwmFlush()</c> e anota.</item>
/// </list>
/// O instante anotado é o da composição pelo lado do DWM (<c>qpcCompose − qpcRefreshPeriod</c> de
/// <c>DwmGetCompositionTimingInfo</c>, ver <see cref="Composition"/>), só se o número da composição (<c>cFrame</c>)
/// avançou desde antes da pintura; senão (ou se o <c>DwmFlush</c> falhou)
/// não vira desenho e conta como falha. No parado, o topo das janelas é reafirmado ~1 s longe de cada troca.
/// </summary>
internal sealed class BenchScreen : IDisposable
{
    private static readonly long StillInterval = Stopwatch.Frequency * 2;

    [DllImport("dwmapi.dll")]
    private static extern int DwmFlush();

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetCompositionTimingInfo(IntPtr hwnd, ref DwmTimingInfo info);

    /// <summary><c>DWM_TIMING_INFO</c> de <c>dwmapi.h</c> (empacotado em 1 byte, <c>pshpack1.h</c>).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct DwmTimingInfo
    {
        public uint CbSize;
        public uint RateRefreshNumerator, RateRefreshDenominator;
        public ulong QpcRefreshPeriod;
        public uint RateComposeNumerator, RateComposeDenominator;
        public ulong QpcVBlank, CRefresh;
        public uint CDXRefresh;
        public ulong QpcCompose, CFrame;
        public uint CDXPresent;
        public ulong CRefreshFrame, CFrameSubmitted;
        public uint CDXPresentSubmitted;
        public ulong CFrameConfirmed;
        public uint CDXPresentConfirmed;
        public ulong CRefreshConfirmed;
        public uint CDXRefreshConfirmed;
        public ulong CFramesLate;
        public uint CFramesOutstanding;
        public ulong CFrameDisplayed, QpcFrameDisplayed, CRefreshFrameDisplayed, CFrameComplete, QpcFrameComplete,
            CFramePending, QpcFramePending, CFramesDisplayed, CFramesComplete, CFramesPending, CFramesAvailable,
            CFramesDropped, CFramesMissed, CRefreshNextDisplayed, CRefreshNextPresented, CRefreshesDisplayed,
            CRefreshesPresented, CRefreshStarted, CPixelsReceived, CPixelsDrawn, CBuffersEmpty;
    }

    /// <summary>
    /// Número da composição e instante da composição que <b>acabou</b> de acontecer (QPC = ticks do <c>Stopwatch</c>);
    /// null se o DWM não informar. Medido neste PC (2026-10-21): logo depois do <c>DwmFlush</c>, <c>qpcCompose</c>
    /// (= <c>qpcVBlank</c>) fica sempre 15,7–16,0 ms no futuro — é a <b>próxima</b> composição; a que mostrou a pintura é
    /// <c>qpcCompose − qpcRefreshPeriod</c>.
    /// </summary>
    private static (ulong Frame, long Qpc)? Composition()
    {
        var info = new DwmTimingInfo { CbSize = (uint)Marshal.SizeOf<DwmTimingInfo>() };
        return DwmGetCompositionTimingInfo(IntPtr.Zero, ref info) >= 0
            ? (info.CFrame, (long)info.QpcCompose - (long)info.QpcRefreshPeriod)
            : null;
    }

    /// <summary>
    /// <c>DwmFlush</c> e o instante da composição que mostrou a pintura feita depois de <paramref name="before"/>, só se o
    /// <c>cFrame</c> avançou e a composição não é anterior a <paramref name="paintedAt"/>; senão conta como falha.
    /// </summary>
    private bool TryComposed((ulong Frame, long Qpc)? before, long paintedAt, out long composedAt)
    {
        composedAt = 0;
        var flushed = DwmFlush() >= 0;
        var returned = Stopwatch.GetTimestamp();
        var after = Composition();
        if (!flushed || before is not { } b || after is not { } a || a.Frame <= b.Frame || a.Qpc < paintedAt)
        {
            _flushFailures++;
            return false;
        }
        if (a.Frame > b.Frame + 1)
        {
            _skippedCompositions++;
        }
        _composeToReturnTicks += returned - a.Qpc;
        composedAt = a.Qpc;
        return true;
    }

    private readonly string _scenario;
    private readonly Rectangle _monitor;
    private readonly ManualResetEventSlim _ready = new();
    private readonly List<DrawEvent> _draws = [];
    private readonly Thread _ui;
    private readonly Thread _loop;
    private ApplicationContext? _context;
    private MarkerWindow? _marker;
    private SceneWindow? _scene;
    private Exception? _uiError;
    private volatile bool _stop;
    private bool _stopped;
    private int _loops;
    private int _flushFailures;
    private int _skippedCompositions;
    private long _composeToReturnTicks;
    private long _loopStart;
    private long _loopEnd;

    private BenchScreen(string scenario, Rectangle monitor)
    {
        _scenario = scenario;
        _monitor = monitor;
        _ui = new Thread(RunUi) { IsBackground = true, Name = "bancada-janelas" };
        _ui.SetApartmentState(ApartmentState.STA);
        _loop = new Thread(Animate) { IsBackground = true, Name = "bancada-marcador", Priority = ThreadPriority.Highest };
    }

    /// <summary>
    /// Abre as janelas em <paramref name="monitor"/> (pixels físicos do desktop) e começa a trocar o marcador. Se não abrir,
    /// fecha o que já tinha sido criado antes de lançar.
    /// </summary>
    public static BenchScreen Start(string scenario, Rectangle monitor)
    {
        var screen = new BenchScreen(scenario, monitor);
        screen._ui.Start();
        if (!screen._ready.Wait(TimeSpan.FromSeconds(10)))
        {
            screen.Dispose();
            throw new InvalidOperationException("as janelas da bancada não abriram em 10 s");
        }
        if (screen._uiError is { } error)
        {
            screen.Dispose();
            throw new InvalidOperationException($"as janelas da bancada não abriram: {error.Message}", error);
        }
        screen._loop.Start();
        return screen;
    }

    /// <summary>Para de trocar, fecha as janelas e devolve os desenhos (código + instante em que foi composto).</summary>
    public IReadOnlyList<DrawEvent> Stop()
    {
        if (!_stopped)
        {
            _stopped = true;
            _stop = true;
            if (_loop.IsAlive)
            {
                _loop.Join();
            }
            try
            {
                if (_marker is { IsHandleCreated: true } marker)
                {
                    marker.Invoke(() => _context?.ExitThread());
                }
            }
            catch (Exception e) when (e is ObjectDisposedException or InvalidOperationException)
            {
            }
            _ui.Join(TimeSpan.FromSeconds(5));
        }
        return _draws;
    }

    /// <summary>Voltas por segundo medidas (desenhos anotados ÷ tempo real da volta) e falhas do DwmFlush, para o relatório.</summary>
    public string LoopSummary()
    {
        var seconds = (_loopEnd - _loopStart) / (double)Stopwatch.Frequency;
        var drawsPerSecond = seconds > 0 ? _draws.Count / seconds : 0;
        var failures = _flushFailures == 0 ? "" : $", sem composição confirmada pelo DWM {_flushFailures} vez(es)";
        var skipped = _skippedCompositions == 0 ? "" : $", {_skippedCompositions} vez(es) mais de 1 composição no meio";
        var lag = _draws.Count == 0 ? 0 : _composeToReturnTicks * 1000.0 / Stopwatch.Frequency / _draws.Count;
        return $"{_draws.Count} desenhos em {seconds:0.0} s = {drawsPerSecond:0.0} voltas/s ({_loops} voltas), " +
            $"DwmFlush volta {lag:0.0} ms depois da composição em média{failures}{skipped}";
    }

    public void Dispose()
    {
        Stop();
        _ready.Dispose();
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hwnd, out WindowRect rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(Point point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

    private struct WindowRect
    {
        public int Left, Top, Right, Bottom;
    }

    /// <summary>
    /// Onde as janelas estão de verdade (posição, visível, sempre por cima) e se outra janela está na frente do centro
    /// delas (<c>COBERTA</c>, por <c>WindowFromPoint</c>), para o relatório.
    /// </summary>
    public string Describe()
    {
        static string One(string name, Form? form)
        {
            if (form is null)
            {
                return "";
            }
            const int exStyle = -20;
            const long topmost = 0x00000008;
            const uint root = 2; // GA_ROOT
            var handle = form.Handle;
            GetWindowRect(handle, out var r);
            var top = ((long)GetWindowLongPtr(handle, exStyle) & topmost) != 0;
            var centre = new Point((r.Left + r.Right) / 2, (r.Top + r.Bottom) / 2);
            var front = WindowFromPoint(centre);
            var covered = front != IntPtr.Zero && GetAncestor(front, root) != handle;
            return $"{name} {r.Left},{r.Top} {r.Right - r.Left}×{r.Bottom - r.Top}" +
                $"{(IsWindowVisible(handle) ? "" : " INVISÍVEL")}{(top ? "" : " SEM TOPMOST")}{(covered ? " COBERTA" : "")}";
        }
        try
        {
            return (string)_marker!.Invoke(() =>
                string.Join("; ", new[] { One("marcador", _marker), One("cena", _scene) }.Where(s => s.Length > 0)));
        }
        catch (Exception e) when (e is ObjectDisposedException or InvalidOperationException)
        {
            return "janelas já fechadas";
        }
    }

    private static Rectangle On(Rectangle monitor, PixelRect r) => new(monitor.X + r.X, monitor.Y + r.Y, r.Width, r.Height);

    private void RunUi()
    {
        try
        {
            _marker = new MarkerWindow(On(_monitor, BenchLayout.Marker));
            _scene = _scenario switch
            {
                BenchScenarios.Scroll => new ScrollWindow(On(_monitor, BenchLayout.Scroll(_monitor.Width, _monitor.Height))),
                BenchScenarios.Video => new NoiseWindow(On(_monitor, BenchLayout.Noise(_monitor.Width, _monitor.Height))),
                _ => null,
            };
            _scene?.Show();
            _marker.Show();
            _context = new ApplicationContext();
        }
        catch (Exception e)
        {
            _uiError = e;
            _scene?.Dispose();
            _marker?.Dispose();
            _ready.Set();
            return;
        }
        _ready.Set();
        try
        {
            Application.Run(_context);
        }
        finally
        {
            _marker.Dispose();
            _scene?.Dispose();
        }
    }

    private void Animate()
    {
        _loopStart = Stopwatch.GetTimestamp();
        try
        {
            if (_scenario == BenchScenarios.Still)
            {
                AnimateStill();
            }
            else
            {
                AnimateEveryFrame();
            }
        }
        finally
        {
            _loopEnd = Stopwatch.GetTimestamp();
        }
    }

    private void AnimateEveryFrame()
    {
        var raiseEvery = Stopwatch.Frequency;
        var lastRaise = 0L;
        var code = 0;
        int? pending = null; // o código 0 pintado ao abrir não é anotado (a 1ª imagem nunca é amostra)
        (ulong Frame, long Qpc)? before = null;
        var paintedAt = 0L;
        while (!_stop)
        {
            if (pending is { } drawn)
            {
                if (TryComposed(before, paintedAt, out var composedAt))
                {
                    _draws.Add(new DrawEvent(drawn, composedAt));
                }
            }
            else if (DwmFlush() < 0)
            {
                Thread.Sleep(1);
            }
            var now = Stopwatch.GetTimestamp();
            code = (code + 1) & 0xFFFF;
            pending = code;
            var raise = now - lastRaise >= raiseEvery;
            if (raise)
            {
                lastRaise = now;
            }
            _loops++;
            before = Composition();
            paintedAt = Stopwatch.GetTimestamp();
            if (!Paint(code, raise, advanceScene: true))
            {
                return;
            }
        }
    }

    private void AnimateStill()
    {
        var code = 0;
        var next = Stopwatch.GetTimestamp() + StillInterval;
        var raised = false;
        while (!_stop)
        {
            var now = Stopwatch.GetTimestamp();
            // Topo reafirmado na metade do intervalo: ~1 s longe da troca anterior e da seguinte.
            if (!raised && now >= next - StillInterval / 2)
            {
                raised = true;
                if (!RaiseWindows())
                {
                    return;
                }
                continue;
            }
            var wait = next - now;
            if (wait > 0)
            {
                Thread.Sleep((int)Math.Clamp(wait * 1000 / Stopwatch.Frequency, 1, 50));
                continue;
            }
            code = (code + 1) & 0xFFFF;
            _loops++;
            var before = Composition();
            var paintedAt = Stopwatch.GetTimestamp();
            if (!Paint(code, raise: false, advanceScene: false))
            {
                return;
            }
            if (TryComposed(before, paintedAt, out var composedAt))
            {
                _draws.Add(new DrawEvent(code, composedAt));
            }
            next += StillInterval;
            raised = false;
        }
    }

    /// <summary>Só reafirma o topo das janelas (sem pintar); <c>false</c> se as janelas já fecharam.</summary>
    private bool RaiseWindows()
    {
        var marker = _marker!;
        var scene = _scene;
        try
        {
            marker.Invoke(() =>
            {
                scene?.Raise();
                marker.Raise();
            });
            return true;
        }
        catch (Exception e) when (e is ObjectDisposedException or InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>Pinta pela thread da interface; <c>false</c> se as janelas já fecharam.</summary>
    private bool Paint(int code, bool raise, bool advanceScene)
    {
        var marker = _marker!;
        var scene = _scene;
        try
        {
            marker.Invoke(() =>
            {
                if (raise)
                {
                    scene?.Raise();
                    marker.Raise();
                }
                marker.Code = code;
                marker.Invalidate();
                marker.Update();
                if (advanceScene && scene is not null)
                {
                    scene.Advance();
                    scene.Invalidate();
                    scene.Update();
                }
            });
            return true;
        }
        catch (Exception e) when (e is ObjectDisposedException or InvalidOperationException)
        {
            return false;
        }
    }
}
