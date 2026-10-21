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
/// O que a bancada mostra no monitor medido: o marcador no canto superior esquerdo + (16, 16) e a cena do cenário. Uma
/// thread de interface (STA) mantém as janelas; uma thread de fundo anda uma volta por composição do DWM:
/// <c>DwmFlush()</c> → anota o desenho anterior (já composto) → troca o código se o cenário pede → pinta pela thread da
/// interface. Só os desenhos que trocaram o código viram <see cref="DrawEvent"/>.
/// </summary>
internal sealed class BenchScreen : IDisposable
{
    private const int StillChanges = 6;

    [DllImport("dwmapi.dll")]
    private static extern int DwmFlush();

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

    private BenchScreen(string scenario, Rectangle monitor)
    {
        _scenario = scenario;
        _monitor = monitor;
        _ui = new Thread(RunUi) { IsBackground = true, Name = "bancada-janelas" };
        _ui.SetApartmentState(ApartmentState.STA);
        _loop = new Thread(Animate) { IsBackground = true, Name = "bancada-marcador", Priority = ThreadPriority.Highest };
    }

    /// <summary>Abre as janelas em <paramref name="monitor"/> (pixels físicos do desktop) e começa a trocar o marcador.</summary>
    public static BenchScreen Start(string scenario, Rectangle monitor)
    {
        var screen = new BenchScreen(scenario, monitor);
        screen._ui.Start();
        if (!screen._ready.Wait(TimeSpan.FromSeconds(10)))
        {
            throw new InvalidOperationException("as janelas da bancada não abriram em 10 s");
        }
        if (screen._uiError is { } error)
        {
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
                _marker?.Invoke(() => _context?.ExitThread());
            }
            catch (Exception e) when (e is ObjectDisposedException or InvalidOperationException)
            {
            }
            _ui.Join(TimeSpan.FromSeconds(5));
        }
        return _draws;
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

    private struct WindowRect
    {
        public int Left, Top, Right, Bottom;
    }

    /// <summary>Onde as janelas estão de verdade (posição, visível, sempre por cima), para o relatório.</summary>
    public string Describe()
    {
        static string One(string name, Form? form)
        {
            if (form is null)
            {
                return "";
            }
            var handle = form.Handle;
            GetWindowRect(handle, out var r);
            const long topmost = 0x00000008;
            var top = ((long)GetWindowLongPtr(handle, -20) & topmost) != 0;
            return $"{name} {r.Left},{r.Top} {r.Right - r.Left}×{r.Bottom - r.Top}{(IsWindowVisible(handle) ? "" : " INVISÍVEL")}{(top ? "" : " SEM TOPMOST")}";
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

    private void RunUi()
    {
        try
        {
            _marker = new MarkerWindow(new Rectangle(
                _monitor.X + MarkerCode.Offset, _monitor.Y + MarkerCode.Offset, MarkerCode.Size, MarkerCode.Size));
            _scene = SceneWindows.Create(_scenario, _monitor);
            _scene?.Show();
            _marker.Show();
            _context = new ApplicationContext();
        }
        catch (Exception e)
        {
            _uiError = e;
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
        var marker = _marker!;
        var scene = _scene;
        var still = _scenario == BenchScenarios.Still;
        var stillInterval = Stopwatch.Frequency * 2;
        var lastChange = Stopwatch.GetTimestamp();
        var changes = 0;
        var code = 0;
        int? pending = 0; // o código 0 já foi pintado ao abrir
        var raiseEvery = Stopwatch.Frequency;
        var lastRaise = 0L;
        while (!_stop)
        {
            if (DwmFlush() < 0)
            {
                Thread.Sleep(1);
            }
            var now = Stopwatch.GetTimestamp();
            if (pending is { } drawn)
            {
                _draws.Add(new DrawEvent(drawn, now));
                pending = null;
            }
            if (!still || (changes < StillChanges && now - lastChange >= stillInterval))
            {
                code = (code + 1) & 0xFFFF;
                pending = code;
                lastChange = now;
                changes++;
            }
            var paint = code;
            var raise = now - lastRaise >= raiseEvery;
            if (raise)
            {
                lastRaise = now;
            }
            try
            {
                // Repinta o marcador sempre (mesmo parado): a janela suja faz o DWM compor e o DwmFlush voltar a cada imagem.
                marker.Invoke(() =>
                {
                    if (raise)
                    {
                        scene?.Raise();
                        marker.Raise();
                    }
                    marker.Code = paint;
                    marker.Invalidate();
                    marker.Update();
                    if (scene is not null)
                    {
                        scene.Advance();
                        scene.Invalidate();
                        scene.Update();
                    }
                });
            }
            catch (Exception e) when (e is ObjectDisposedException or InvalidOperationException)
            {
                return;
            }
        }
    }
}
