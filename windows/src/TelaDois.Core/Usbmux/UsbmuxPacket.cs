using System.Buffers.Binary;

namespace TelaDois.Core.Usbmux;

/// <summary>Pacote do usbmuxd: cabeçalho de 16 bytes little-endian (tamanho, versão 1, tipo 8 = plist, tag) + plist.</summary>
public static class UsbmuxPacket
{
    public const int HeaderSize = 16;
    public const uint ProtocolVersion = 1;
    public const uint PlistMessageType = 8;
    private const int MaxSize = 1024 * 1024;

    public static byte[] Build(uint tag, IReadOnlyDictionary<string, object> body)
    {
        var plist = Plist.Serialize(body);
        var packet = new byte[HeaderSize + plist.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(packet, (uint)packet.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(4), ProtocolVersion);
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(8), PlistMessageType);
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(12), tag);
        plist.CopyTo(packet, HeaderSize);
        return packet;
    }

    public static async Task<(uint Tag, Dictionary<string, object> Body)> ReadAsync(Stream stream, CancellationToken ct)
    {
        var header = new byte[HeaderSize];
        await stream.ReadExactlyAsync(header, ct);
        var length = BinaryPrimitives.ReadUInt32LittleEndian(header);
        if (length < HeaderSize || length > MaxSize)
        {
            throw new InvalidDataException($"pacote do usbmuxd com tamanho inválido: {length}");
        }
        var body = new byte[length - HeaderSize];
        await stream.ReadExactlyAsync(body, ct);
        return (BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(12)), Plist.Parse(body));
    }

    /// <summary>O usbmuxd espera a porta com os bytes trocados (ordem de rede lida como little-endian).</summary>
    public static ushort NetworkPort(ushort port) => BinaryPrimitives.ReverseEndianness(port);
}
