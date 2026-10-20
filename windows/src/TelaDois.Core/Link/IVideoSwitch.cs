namespace TelaDois.Core.Link;

/// <summary>Diz à sessão se o vídeo deve estar ligado; a sessão avisa quando o vídeo parou sozinho.</summary>
public interface IVideoSwitch
{
    bool WantsVideo { get; }

    /// <summary>Completa quando o estado deixar de ser <paramref name="seen"/> (na hora, se já for diferente).</summary>
    Task WaitForChangeAsync(bool seen, CancellationToken ct);

    /// <summary>O vídeo parou ou não abriu: passa a desligado (o atalho pode ligar de novo).</summary>
    void VideoEnded();
}
