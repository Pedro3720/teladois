/// Tipos de NAL do H.264 usados na transmissão.
public enum H264NALType {
    public static let idr: UInt8 = 5
    public static let sps: UInt8 = 7
    public static let pps: UInt8 = 8
    public static let aud: UInt8 = 9
}

/// Uma unidade NAL sem o código de início.
public struct H264NALUnit: Equatable, Sendable {
    public let type: UInt8
    public let bytes: [UInt8]

    public init(type: UInt8, bytes: [UInt8]) {
        self.type = type
        self.bytes = bytes
    }

    /// NAL com dados de imagem (fatias 1…5).
    public var isVCL: Bool { (1...5).contains(type) }
}

/// Lê H.264 Annex-B (códigos de início `00 00 01` / `00 00 00 01`) como o FFmpeg do PC entrega.
public enum H264Parser {
    public static func nalUnits(in stream: [UInt8]) -> [H264NALUnit] {
        var starts: [(code: Int, payload: Int)] = []
        var index = 0
        while index + 2 < stream.count {
            if stream[index] == 0, stream[index + 1] == 0, stream[index + 2] == 1 {
                let code = index > 0 && stream[index - 1] == 0 ? index - 1 : index
                starts.append((code, index + 3))
                index += 3
            } else {
                index += 1
            }
        }
        var units: [H264NALUnit] = []
        for (position, start) in starts.enumerated() {
            let end = position + 1 < starts.count ? starts[position + 1].code : stream.count
            guard start.payload < end else { continue }
            let bytes = Array(stream[start.payload..<end])
            units.append(H264NALUnit(type: bytes[0] & 0x1F, bytes: bytes))
        }
        return units
    }

    /// Imagens (unidades de acesso) separadas pelos AUD, em Annex-B; descarta as que não têm dados de imagem.
    public static func accessUnits(in stream: [UInt8]) -> [[UInt8]] {
        var result: [[UInt8]] = []
        var current: [H264NALUnit] = []
        func close() {
            if current.contains(where: \.isVCL) { result.append(annexB(current)) }
            current = []
        }
        for nal in nalUnits(in: stream) {
            if nal.type == H264NALType.aud { close() }
            current.append(nal)
        }
        close()
        return result
    }

    /// Formato do `CMSampleBuffer`: cada NAL com tamanho de 4 bytes; sem AUD, SPS e PPS (vão no formato).
    public static func avcc(_ nals: [H264NALUnit]) -> [UInt8] {
        var bytes: [UInt8] = []
        for nal in nals where ![H264NALType.aud, H264NALType.sps, H264NALType.pps].contains(nal.type) {
            bytes += bigEndianBytes(UInt32(nal.bytes.count)) + nal.bytes
        }
        return bytes
    }

    public static func containsIDR(_ nals: [H264NALUnit]) -> Bool {
        nals.contains { $0.type == H264NALType.idr }
    }

    private static func annexB(_ nals: [H264NALUnit]) -> [UInt8] {
        nals.flatMap { [0, 0, 0, 1] + $0.bytes }
    }
}
