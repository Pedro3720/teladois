namespace TelaDois.Core.Link;

/// <summary>Lê mensagens de um stream; <c>null</c> quando o outro lado fecha a conexão.</summary>
public sealed class LinkFrameReader(Stream stream)
{
    private readonly LinkFrameDecoder _decoder = new();
    private readonly Queue<LinkFrame> _pending = new();
    private readonly byte[] _buffer = new byte[64 * 1024];

    public async Task<LinkFrame?> NextAsync(CancellationToken ct)
    {
        while (_pending.Count == 0)
        {
            var read = await stream.ReadAsync(_buffer, ct);
            if (read == 0)
            {
                return null;
            }
            foreach (var frame in _decoder.Append(_buffer.AsSpan(0, read)))
            {
                _pending.Enqueue(frame);
            }
        }
        return _pending.Dequeue();
    }
}
