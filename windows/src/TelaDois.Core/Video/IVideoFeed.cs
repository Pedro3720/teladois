using TelaDois.Core.Link;

namespace TelaDois.Core.Video;

/// <summary>Fonte de vídeo de uma sessão: formato fixo e imagens em sequência. Descartar encerra a captura.</summary>
public interface IVideoFeed : IAsyncDisposable
{
    VideoConfig Config { get; }
    Task<AccessUnit> NextAsync(CancellationToken ct);
}
