namespace TelaDois.Core.Display;

/// <summary>
/// Liga/desliga o dispositivo do Virtual Display Driver (desativado, o Windows deixa de listar o monitor). Quem faz é uma
/// tarefa do Windows que roda como SYSTEM, criada pelo instalador: o TelaDois não precisa de administrador.
/// </summary>
public interface IDeviceSwitch
{
    /// <summary>As tarefas de ligar e de desligar existem. Nunca lança (exceto o cancelamento de <paramref name="ct"/>).</summary>
    Task<bool> IsAvailableAsync(CancellationToken ct);

    /// <summary>
    /// Dispara a tarefa de ligar (<paramref name="enable"/>) ou desligar e devolve se o Windows aceitou o disparo; não espera
    /// o dispositivo mudar. Nunca lança (exceto o cancelamento de <paramref name="ct"/>).
    /// </summary>
    Task<bool> RunAsync(bool enable, CancellationToken ct);
}
