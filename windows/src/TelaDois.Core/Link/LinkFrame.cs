using System.Buffers.Binary;

namespace TelaDois.Core.Link;

/// <summary>Tipos de mensagem do protocolo PC ↔ iPhone (spec §3.4).</summary>
public enum LinkMessageType : byte
{
    Hello = 1,
    Config = 2,
    Video = 3,
    KeyframeRequest = 4,
    Ping = 5,
    Pong = 6,
    Bye = 7,
    VideoEnd = 8,
    /// <summary>iPhone → PC, 1 vez por segundo com a Segunda Tela ligada: medidores do último intervalo.</summary>
    Stats = 9,
}

/// <summary>Uma mensagem: <c>[tipo: 1 byte][tamanho: 4 bytes big-endian][dados]</c>.</summary>
public sealed record LinkFrame(byte Type, byte[] Payload)
{
    public const int HeaderSize = 5;
    public const int MaxPayload = 16 * 1024 * 1024;

    public LinkFrame(LinkMessageType type, byte[]? payload = null) : this((byte)type, payload ?? []) { }

    /// <summary><c>null</c> para tipos desconhecidos (versões futuras), que são ignorados.</summary>
    public LinkMessageType? MessageType =>
        Enum.IsDefined((LinkMessageType)Type) ? (LinkMessageType)Type : null;

    public byte[] Encode()
    {
        var bytes = new byte[HeaderSize + Payload.Length];
        bytes[0] = Type;
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(1), (uint)Payload.Length);
        Payload.CopyTo(bytes, HeaderSize);
        return bytes;
    }
}
