import Foundation

/// Tipos de mensagem do protocolo PC ↔ iPhone (spec §3.4).
public enum LinkMessageType: UInt8, Sendable {
    case hello = 1, config, video, keyframeRequest, ping, pong, bye
}

/// Uma mensagem: `[tipo: 1 byte][tamanho: 4 bytes big-endian][dados]`.
public struct LinkFrame: Equatable, Sendable {
    public static let headerSize = 5
    public let type: UInt8
    public let payload: [UInt8]

    public init(type: UInt8, payload: [UInt8] = []) {
        self.type = type
        self.payload = payload
    }

    public init(_ type: LinkMessageType, payload: [UInt8] = []) {
        self.init(type: type.rawValue, payload: payload)
    }

    /// `nil` para tipos desconhecidos (versões futuras), que são ignorados.
    public var messageType: LinkMessageType? { LinkMessageType(rawValue: type) }

    public func encoded() -> [UInt8] {
        [type] + bigEndianBytes(UInt32(payload.count)) + payload
    }
}

public struct LinkHello: Equatable, Sendable {
    public let version: UInt16
    public let width: UInt32
    public let height: UInt32

    public init(version: UInt16, width: UInt32, height: UInt32) {
        self.version = version
        self.width = width
        self.height = height
    }
}

/// Formato do vídeo transmitido pelo PC.
public struct LinkVideoConfig: Equatable, Sendable {
    public let width: UInt32
    public let height: UInt32
    public let fps: UInt16

    public init(width: UInt32, height: UInt32, fps: UInt16) {
        self.width = width
        self.height = height
        self.fps = fps
    }
}

/// Uma imagem H.264 (Annex-B); `timestamp` em µs desde o início da transmissão.
public struct LinkVideoFrame: Equatable, Sendable {
    public let isKeyframe: Bool
    public let timestamp: UInt64
    public let accessUnit: [UInt8]

    public init(isKeyframe: Bool, timestamp: UInt64, accessUnit: [UInt8]) {
        self.isKeyframe = isKeyframe
        self.timestamp = timestamp
        self.accessUnit = accessUnit
    }
}

public enum LinkProtocol {
    public static let version: UInt16 = 1
    public static let port: UInt16 = 7100
    public static let maxPayload = 16 * 1024 * 1024
    /// Sem receber nada por este tempo, a conexão é encerrada.
    public static let timeout: TimeInterval = 3

    public static func hello(width: UInt32, height: UInt32) -> LinkFrame {
        LinkFrame(.hello, payload: bigEndianBytes(version) + bigEndianBytes(width) + bigEndianBytes(height))
    }

    public static func parseHello(_ frame: LinkFrame) -> LinkHello? {
        guard frame.messageType == .hello, frame.payload.count == 10 else { return nil }
        let bytes = frame.payload[...]
        return LinkHello(
            version: readBigEndian(bytes[0..<2]),
            width: readBigEndian(bytes[2..<6]),
            height: readBigEndian(bytes[6..<10])
        )
    }

    public static func config(width: UInt32, height: UInt32, fps: UInt16) -> LinkFrame {
        LinkFrame(.config, payload: bigEndianBytes(width) + bigEndianBytes(height) + bigEndianBytes(fps))
    }

    public static func parseConfig(_ frame: LinkFrame) -> LinkVideoConfig? {
        guard frame.messageType == .config, frame.payload.count == 10 else { return nil }
        let bytes = frame.payload[...]
        return LinkVideoConfig(
            width: readBigEndian(bytes[0..<4]),
            height: readBigEndian(bytes[4..<8]),
            fps: readBigEndian(bytes[8..<10])
        )
    }

    public static func video(keyframe: Bool, timestamp: UInt64, accessUnit: [UInt8]) -> LinkFrame {
        LinkFrame(.video, payload: [keyframe ? 1 : 0] + bigEndianBytes(timestamp) + accessUnit)
    }

    public static func parseVideo(_ frame: LinkFrame) -> LinkVideoFrame? {
        guard frame.messageType == .video, frame.payload.count >= 9 else { return nil }
        let bytes = frame.payload[...]
        return LinkVideoFrame(
            isKeyframe: bytes[0] & 1 != 0,
            timestamp: readBigEndian(bytes[1..<9]),
            accessUnit: Array(bytes[9...])
        )
    }

    public static func ping(_ timestamp: UInt64) -> LinkFrame {
        LinkFrame(.ping, payload: bigEndianBytes(timestamp))
    }

    public static func pong(_ timestamp: UInt64) -> LinkFrame {
        LinkFrame(.pong, payload: bigEndianBytes(timestamp))
    }

    public static func bye() -> LinkFrame { LinkFrame(.bye) }

    /// Carimbo de tempo de um `PING` ou `PONG`.
    public static func timestamp(of frame: LinkFrame) -> UInt64? {
        guard frame.messageType == .ping || frame.messageType == .pong, frame.payload.count == 8 else { return nil }
        return readBigEndian(frame.payload[...])
    }
}

public enum LinkDecodeError: Error, Equatable {
    case payloadTooLarge(UInt32)
}

/// Junta os bytes que chegam em pedaços e devolve as mensagens completas.
public struct LinkFrameDecoder: Sendable {
    private var buffer: [UInt8] = []

    public init() {}

    public mutating func append(_ bytes: some Sequence<UInt8>) throws -> [LinkFrame] {
        buffer.append(contentsOf: bytes)
        var frames: [LinkFrame] = []
        var offset = 0
        while buffer.count - offset >= LinkFrame.headerSize {
            let length: UInt32 = readBigEndian(buffer[(offset + 1)..<(offset + 5)])
            guard length <= LinkProtocol.maxPayload else { throw LinkDecodeError.payloadTooLarge(length) }
            let end = offset + LinkFrame.headerSize + Int(length)
            guard buffer.count >= end else { break }
            frames.append(LinkFrame(type: buffer[offset], payload: Array(buffer[(offset + LinkFrame.headerSize)..<end])))
            offset = end
        }
        if offset > 0 { buffer.removeFirst(offset) }
        return frames
    }
}

func bigEndianBytes<T: FixedWidthInteger>(_ value: T) -> [UInt8] {
    withUnsafeBytes(of: value.bigEndian) { Array($0) }
}

func readBigEndian<T: FixedWidthInteger>(_ bytes: ArraySlice<UInt8>) -> T {
    bytes.reduce(T.zero) { ($0 << 8) | T($1) }
}
