using TelaDois.Core.Link;

namespace TelaDois.Core.Video;

/// <summary>De onde vem o vídeo da Segunda Tela (monitor virtual + FFmpeg; nos testes, uma fonte falsa).</summary>
public interface IVideoSource
{
    Task<IVideoStream> StartAsync(HelloInfo hello, CancellationToken ct);
}

public interface IVideoStream : IAsyncDisposable
{
    VideoConfig Config { get; }

    /// <summary>Access units H.264 Annex-B, um por quadro, até o token ser cancelado ou a captura parar.</summary>
    IAsyncEnumerable<byte[]> AccessUnitsAsync(CancellationToken ct);

    void RequestKeyframe();
}
