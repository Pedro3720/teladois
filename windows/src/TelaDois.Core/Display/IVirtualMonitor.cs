namespace TelaDois.Core.Display;

/// <summary>O monitor extra da Segunda Tela, como a fábrica de vídeo o usa.</summary>
public interface IVirtualMonitor
{
    Task<bool> IsInstalledAsync(CancellationToken ct);

    /// <summary>
    /// Ativa o dispositivo do driver se o Windows não lista o monitor e espera ele aparecer; <c>true</c> quando o monitor
    /// está presente (<see cref="Find"/> não nulo) ao fim. Só lança o cancelamento de <paramref name="ct"/>.
    /// </summary>
    Task<bool> PowerOnAsync(CancellationToken ct);

    /// <summary>Desativa o dispositivo do driver (só se as tarefas existem) e espera o monitor sumir. Só lança o cancelamento.</summary>
    Task PowerOffAsync(CancellationToken ct);

    /// <summary>As tarefas que ativam/desativam o dispositivo existem.</summary>
    Task<bool> CanPowerOffAsync(CancellationToken ct);

    /// <summary><c>\\.\DISPLAYn</c> do monitor virtual, ligado ou não; <c>null</c> sem ele (Duplicar nunca o escolhe).</summary>
    string? Find();

    /// <summary>Põe o monitor no desktop em Estender com o modo mais próximo de <paramref name="width"/>×<paramref name="height"/>.</summary>
    /// <exception cref="InvalidOperationException">O monitor do driver não aparece no Windows.</exception>
    /// <exception cref="DisplayChangeException">O Windows recusou a mudança.</exception>
    (string DeviceName, DisplayModeInfo Mode) Attach(int width, int height);

    void Detach(string deviceName);

    bool IsAttached(string deviceName);

    /// <summary>
    /// DPI efetivo do monitor virtual no desktop (96 = escala 100%); <c>null</c> se o Windows não informou ou se
    /// <paramref name="deviceName"/> não é o monitor virtual. Uma falha da API do Windows sai como exceção: quem chama
    /// registra numa linha só, com o motivo.
    /// </summary>
    int? EffectiveDpi(string deviceName);

    /// <summary>Os modos que o monitor virtual informa (os do <c>vdd_settings.xml</c>); vazio sem ele ou se o Windows falhou. Nunca lança.</summary>
    IReadOnlyList<DisplayModeInfo> Modes();
}
