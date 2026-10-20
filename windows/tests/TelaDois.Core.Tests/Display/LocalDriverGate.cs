namespace TelaDois.Core.Tests.Display;

/// <summary>
/// Trava dos testes que mexem no Virtual Display Driver de verdade (pisca e remove um monitor): só rodam quando
/// <c>TELADOIS_LOCAL_DRIVER_TEST=1</c>, nunca por acaso num <c>dotnet test</c> comum.
/// </summary>
internal static class LocalDriverGate
{
    public const string Variable = "TELADOIS_LOCAL_DRIVER_TEST";

    public static bool IsEnabled(string? value) => value == "1";

    public static bool IsEnabled() => IsEnabled(Environment.GetEnvironmentVariable(Variable));
}
