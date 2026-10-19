using TelaDois.Core.Link;

namespace TelaDois.Core.Video;

/// <summary>
/// Fonte de vídeo de uma sessão: formato fixo e imagens em sequência. Descartar encerra a captura (pode ser chamado
/// mais de uma vez) e faz <see cref="NextAsync"/> — pendente ou futuro — terminar com <see cref="OperationCanceledException"/>.
/// </summary>
public interface IVideoFeed : IAsyncDisposable
{
    VideoConfig Config { get; }
    Task<AccessUnit> NextAsync(CancellationToken ct);
}
