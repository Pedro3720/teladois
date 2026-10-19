using System.Runtime.CompilerServices;
using TelaDois.Core.Link;
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Fakes;

internal sealed class FakeVideoSource(int frames) : IVideoSource
{
    public HelloInfo? StartedWith { get; private set; }

    public FakeVideoStream? Stream { get; private set; }

    public Task<IVideoStream> StartAsync(HelloInfo hello, CancellationToken ct)
    {
        StartedWith = hello;
        Stream = new FakeVideoStream(new VideoConfig(hello.Width, hello.Height, 60), frames);
        return Task.FromResult<IVideoStream>(Stream);
    }
}

internal sealed class FakeVideoStream(VideoConfig config, int frames) : IVideoStream
{
    private int _keyframeRequests;
    private volatile bool _disposed;

    public VideoConfig Config => config;

    public int KeyframeRequests => Volatile.Read(ref _keyframeRequests);

    public bool Disposed => _disposed;

    public async IAsyncEnumerable<byte[]> AccessUnitsAsync([EnumeratorCancellation] CancellationToken ct)
    {
        for (var i = 0; i < frames; i++)
        {
            yield return [0, 0, 0, 1, 9, 0xF0, 0, 0, 1, 0x41, (byte)i];
        }
        // Depois dos quadros fica "capturando" até a sessão terminar.
        await Task.Delay(Timeout.Infinite, ct);
    }

    public void RequestKeyframe() => Interlocked.Increment(ref _keyframeRequests);

    public ValueTask DisposeAsync()
    {
        _disposed = true;
        return ValueTask.CompletedTask;
    }
}
