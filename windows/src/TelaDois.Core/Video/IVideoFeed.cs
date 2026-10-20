using TelaDois.Core.Link;

namespace TelaDois.Core.Video;

/// <summary>Duplicar um monitor existente (5a) ou Estender com o monitor virtual (5b).</summary>
public enum VideoKind
{
    Mirror,
    Extend,
}

/// <summary>
/// Fonte de vídeo de uma sessão: formato fixo e imagens em sequência. Descartar encerra a captura (pode ser chamado
/// mais de uma vez) e faz <see cref="NextAsync"/> — pendente ou futuro — terminar com <see cref="OperationCanceledException"/>.
/// <see cref="NextAsync"/> também termina assim quando a fonte para sozinha (ex.: o monitor virtual saiu do desktop).
/// </summary>
public interface IVideoFeed : IAsyncDisposable
{
    VideoConfig Config { get; }

    VideoKind Kind => VideoKind.Mirror;

    Task<AccessUnit> NextAsync(CancellationToken ct);
}
