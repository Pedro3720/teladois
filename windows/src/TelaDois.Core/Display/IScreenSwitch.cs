namespace TelaDois.Core.Link;

/// <summary>Diz à sessão se a tela do iPhone deve ficar preta (com a Segunda Tela ligada); a sessão avisa o toque que acendeu.</summary>
public interface IScreenSwitch
{
    bool WantsDark { get; }

    /// <summary>Completa quando <see cref="WantsDark"/> deixar de ser <paramref name="seen"/> (na hora, se já for diferente).</summary>
    Task WaitForDarkChangeAsync(bool seen, CancellationToken ct);

    /// <summary>O iPhone acendeu sozinho (1 toque): passa a acesa, sem mandar nada de volta.</summary>
    void PhoneWoke();
}
