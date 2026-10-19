namespace TelaDois.Core;

/// <summary>Texto curto da bandeja (o Windows corta o tooltip do ícone em 63 caracteres).</summary>
public static class TunnelStatusText
{
    public static string For(TunnelStatus status) => status.State switch
    {
        TunnelState.ServiceMissing => "TelaDois: falta o Apple Mobile Device Service",
        TunnelState.WaitingForIPhone => "TelaDois: conecte o iPhone no cabo",
        TunnelState.WaitingForApp => "TelaDois: abra o app no iPhone",
        _ when status is { Hello: { } hello, RoundTrip: { } rtt } =>
            $"TelaDois: conectado · {hello.Width}×{hello.Height} · {Math.Round(rtt.TotalMilliseconds):0} ms",
        _ when status.Hello is { } hello => $"TelaDois: conectado · {hello.Width}×{hello.Height}",
        _ => "TelaDois: conectado",
    };
}
