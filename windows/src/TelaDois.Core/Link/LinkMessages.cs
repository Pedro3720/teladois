using System.Buffers.Binary;

namespace TelaDois.Core.Link;

public readonly record struct HelloInfo(ushort Version, uint Width, uint Height);

public static class LinkMessages
{
    public const ushort ProtocolVersion = 1;
    public const ushort Port = 7100;

    public static LinkFrame Hello(uint width, uint height)
    {
        var payload = new byte[10];
        BinaryPrimitives.WriteUInt16BigEndian(payload, ProtocolVersion);
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(2), width);
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(6), height);
        return new LinkFrame(LinkMessageType.Hello, payload);
    }

    public static bool TryParseHello(LinkFrame frame, out HelloInfo hello)
    {
        hello = default;
        if (frame.MessageType != LinkMessageType.Hello || frame.Payload.Length != 10)
        {
            return false;
        }
        var payload = frame.Payload.AsSpan();
        hello = new HelloInfo(
            BinaryPrimitives.ReadUInt16BigEndian(payload),
            BinaryPrimitives.ReadUInt32BigEndian(payload[2..]),
            BinaryPrimitives.ReadUInt32BigEndian(payload[6..]));
        return true;
    }

    public static LinkFrame Ping(ulong timestamp) => Timestamped(LinkMessageType.Ping, timestamp);

    public static LinkFrame Pong(ulong timestamp) => Timestamped(LinkMessageType.Pong, timestamp);

    public static LinkFrame Bye() => new(LinkMessageType.Bye);

    /// <summary>Carimbo de tempo de um PING ou PONG.</summary>
    public static bool TryReadTimestamp(LinkFrame frame, out ulong timestamp)
    {
        timestamp = 0;
        if (frame.MessageType is not (LinkMessageType.Ping or LinkMessageType.Pong) || frame.Payload.Length != 8)
        {
            return false;
        }
        timestamp = BinaryPrimitives.ReadUInt64BigEndian(frame.Payload);
        return true;
    }

    private static LinkFrame Timestamped(LinkMessageType type, ulong timestamp)
    {
        var payload = new byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(payload, timestamp);
        return new LinkFrame(type, payload);
    }
}
