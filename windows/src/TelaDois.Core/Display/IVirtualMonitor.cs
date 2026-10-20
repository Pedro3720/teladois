namespace TelaDois.Core.Display;

/// <summary>O monitor extra da Segunda Tela, como a fábrica de vídeo o usa.</summary>
public interface IVirtualMonitor
{
    Task<bool> IsInstalledAsync(CancellationToken ct);

    /// <summary>Põe o monitor no desktop em Estender com o modo mais próximo de <paramref name="width"/>×<paramref name="height"/>.</summary>
    /// <exception cref="InvalidOperationException">O monitor do driver não aparece no Windows.</exception>
    /// <exception cref="DisplayChangeException">O Windows recusou a mudança.</exception>
    (string DeviceName, DisplayModeInfo Mode) Attach(int width, int height);

    void Detach(string deviceName);

    bool IsAttached(string deviceName);
}
