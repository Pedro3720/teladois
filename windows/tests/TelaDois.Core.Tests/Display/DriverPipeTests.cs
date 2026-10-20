using System.IO.Pipes;
using System.Text;
using TelaDois.Core.Display;

namespace TelaDois.Core.Tests.Display;

public class DriverPipeTests
{
    private static string UniqueName() => $"teladois-teste-{Guid.NewGuid():N}";

    /// <summary>Faz o papel do driver: lê o comando (UTF-16) e responde <paramref name="reply"/> em ASCII.</summary>
    private static async Task<string> ServeOnceAsync(NamedPipeServerStream server, string reply)
    {
        await server.WaitForConnectionAsync();
        var buffer = new byte[256];
        var read = await server.ReadAsync(buffer);
        await server.WriteAsync(Encoding.ASCII.GetBytes(reply));
        await server.FlushAsync();
        return Encoding.Unicode.GetString(buffer, 0, read);
    }

    [Fact]
    public async Task TrueWhenTheDriverAnswersPong()
    {
        var name = UniqueName();
        await using var server = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        var serving = ServeOnceAsync(server, "PONG");
        Assert.True(await new DriverPipe(name).PingAsync(CancellationToken.None));
        Assert.Equal("PING", await serving.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task FalseWhenThePipeAnswersSomethingElse()
    {
        var name = UniqueName();
        var server = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        var serving = Task.Run(async () =>
        {
            await ServeOnceAsync(server, "ERRO");
            await server.DisposeAsync(); // fecha sem PONG
        });
        Assert.False(await new DriverPipe(name).PingAsync(CancellationToken.None));
        await serving.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task FalseWithoutTheDriver() =>
        Assert.False(await new DriverPipe(UniqueName()) { Timeout = TimeSpan.FromMilliseconds(200) }.PingAsync(CancellationToken.None));
}
