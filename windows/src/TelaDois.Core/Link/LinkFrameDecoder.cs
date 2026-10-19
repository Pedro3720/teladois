using System.Buffers.Binary;

namespace TelaDois.Core.Link;

/// <summary>Junta os bytes que chegam em pedaços e devolve as mensagens completas.</summary>
public sealed class LinkFrameDecoder
{
    private byte[] _buffer = new byte[64 * 1024];
    private int _count;

    public IReadOnlyList<LinkFrame> Append(ReadOnlySpan<byte> data)
    {
        if (_count + data.Length > _buffer.Length)
        {
            Array.Resize(ref _buffer, Math.Max(_count + data.Length, _buffer.Length * 2));
        }
        data.CopyTo(_buffer.AsSpan(_count));
        _count += data.Length;

        var frames = new List<LinkFrame>();
        var offset = 0;
        while (_count - offset >= LinkFrame.HeaderSize)
        {
            var length = BinaryPrimitives.ReadUInt32BigEndian(_buffer.AsSpan(offset + 1));
            if (length > LinkFrame.MaxPayload)
            {
                throw new InvalidDataException($"mensagem grande demais: {length} bytes");
            }
            var total = LinkFrame.HeaderSize + (int)length;
            if (_count - offset < total)
            {
                break;
            }
            frames.Add(new LinkFrame(_buffer[offset], _buffer.AsSpan(offset + LinkFrame.HeaderSize, (int)length).ToArray()));
            offset += total;
        }
        if (offset > 0)
        {
            Buffer.BlockCopy(_buffer, offset, _buffer, 0, _count - offset);
            _count -= offset;
        }
        return frames;
    }
}
