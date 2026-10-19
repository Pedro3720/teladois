using TelaDois.Core.Link;

namespace TelaDois.Core.Tests.Fakes;

/// <summary>Faz o papel do app do iPhone do outro lado do túnel.</summary>
internal static class FakePhone
{
    public static async Task RespondAsync(Stream stream, CancellationToken ct)
    {
        await stream.WriteAsync(LinkMessages.Hello(2556, 1179).Encode(), ct);
        var reader = new LinkFrameReader(stream);
        while (await reader.NextAsync(ct) is { } frame)
        {
            if (frame.MessageType == LinkMessageType.Ping && LinkMessages.TryReadTimestamp(frame, out var sent))
            {
                await stream.WriteAsync(LinkMessages.Pong(sent).Encode(), ct);
            }
            if (frame.MessageType == LinkMessageType.Bye)
            {
                return;
            }
        }
    }

    /// <summary>Responde <c>pongs</c> PINGs e então manda BYE (o app sai da tela, por exemplo).</summary>
    public static async Task ByeAfterPongsAsync(Stream stream, int pongs, CancellationToken ct)
    {
        await stream.WriteAsync(LinkMessages.Hello(2556, 1179).Encode(), ct);
        var reader = new LinkFrameReader(stream);
        while (pongs > 0 && await reader.NextAsync(ct) is { } frame)
        {
            if (frame.MessageType == LinkMessageType.Ping && LinkMessages.TryReadTimestamp(frame, out var sent))
            {
                await stream.WriteAsync(LinkMessages.Pong(sent).Encode(), ct);
                pongs--;
            }
        }
        await stream.WriteAsync(LinkMessages.Bye().Encode(), ct);
    }

    public static async Task SilentAfterHelloAsync(Stream stream, CancellationToken ct)
    {
        await stream.WriteAsync(LinkMessages.Hello(2556, 1179).Encode(), ct);
        await Task.Delay(Timeout.Infinite, ct);
    }
}
