using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace TelaDois.LatencyBench;

/// <summary>Uma cena que muda uma vez por volta do marcador (<see cref="Advance"/> roda na thread da interface).</summary>
internal abstract class SceneWindow(Rectangle bounds) : BenchForm(bounds)
{
    public abstract void Advance();
}

internal static class SceneWindows
{
    /// <summary>Distância da cena à borda esquerda do monitor: depois do marcador (16 + 64 + 16).</summary>
    public const int Left = MarkerCode.Offset * 2 + MarkerCode.Size;

    /// <summary>A cena do cenário em <paramref name="monitor"/>; <c>null</c> no cenário parado (só o marcador).</summary>
    public static SceneWindow? Create(string scenario, Rectangle monitor) => scenario switch
    {
        BenchScenarios.Scroll => new ScrollWindow(new Rectangle(monitor.X + Left, monitor.Y, monitor.Width / 3, monitor.Height)),
        BenchScenarios.Video => new NoiseWindow(new Rectangle(monitor.X + Left, monitor.Y + MarkerCode.Offset, monitor.Width / 2, monitor.Height / 2)),
        _ => null,
    };
}

/// <summary>Texto (Segoe UI 16) rolando 4 px por volta, como uma página rolando.</summary>
internal sealed class ScrollWindow : SceneWindow
{
    private const string Phrase = "o rato roeu a roupa do rei de Roma";
    private const int Step = 4;

    private readonly Font _font = new("Segoe UI", 16f);
    private readonly int _lineHeight;
    private int _offset;

    public ScrollWindow(Rectangle bounds) : base(bounds)
    {
        _lineHeight = TextRenderer.MeasureText("Linha 0", _font).Height + 4;
    }

    public override void Advance() => _offset += Step;

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Color.White);
        var line = _offset / _lineHeight;
        for (var y = -(_offset % _lineHeight); y < ClientSize.Height; y += _lineHeight, line++)
        {
            TextRenderer.DrawText(e.Graphics, $"Linha {line + 1} — {Phrase}", _font, new Point(8, y), Color.Black, TextFormatFlags.NoPrefix);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _font.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>Ruído aleatório novo a cada volta (um vídeo que a compressão não prevê), num bitmap de 1/4 do tamanho esticado.</summary>
internal sealed class NoiseWindow : SceneWindow
{
    private readonly Bitmap _noise;
    private readonly byte[] _bytes;
    private readonly int _stride;

    public NoiseWindow(Rectangle bounds) : base(bounds)
    {
        _noise = new Bitmap(Math.Max(1, bounds.Width / 4), Math.Max(1, bounds.Height / 4), PixelFormat.Format32bppRgb);
        var data = _noise.LockBits(new Rectangle(Point.Empty, _noise.Size), ImageLockMode.WriteOnly, _noise.PixelFormat);
        _stride = data.Stride;
        _noise.UnlockBits(data);
        _bytes = new byte[_stride * _noise.Height];
    }

    public override void Advance()
    {
        Random.Shared.NextBytes(_bytes);
        var data = _noise.LockBits(new Rectangle(Point.Empty, _noise.Size), ImageLockMode.WriteOnly, _noise.PixelFormat);
        try
        {
            Marshal.Copy(_bytes, 0, data.Scan0, _stride * _noise.Height);
        }
        finally
        {
            _noise.UnlockBits(data);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
        e.Graphics.DrawImage(_noise, ClientRectangle);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _noise.Dispose();
        }
        base.Dispose(disposing);
    }
}
