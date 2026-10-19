/// H.264 em Annex-B (NALs separadas por `00 00 01` ou `00 00 00 01`), como o FFmpeg entrega.
public enum H264 {
    public static let sliceType: UInt8 = 1
    public static let idrType: UInt8 = 5
    public static let spsType: UInt8 = 7
    public static let ppsType: UInt8 = 8
    public static let audType: UInt8 = 9

    /// NALs de um trecho Annex-B, sem os códigos de início.
    public static func nalUnits(in bytes: [UInt8]) -> [ArraySlice<UInt8>] {
        var units: [ArraySlice<UInt8>] = []
        var start: Int?
        var index = 0
        while index + 2 < bytes.count {
            if bytes[index] == 0, bytes[index + 1] == 0, bytes[index + 2] == 1 {
                if let start { units.append(trimmingTrailingZeros(bytes[start..<index])) }
                index += 3
                start = index
            } else {
                index += 1
            }
        }
        if let start, start < bytes.count { units.append(bytes[start..<bytes.count]) }
        return units.filter { !$0.isEmpty }
    }

    public static func type(of nal: ArraySlice<UInt8>) -> UInt8 {
        (nal.first ?? 0) & 0x1F
    }

    /// AVCC: cada NAL precedida do tamanho em 4 bytes big-endian (formato do `CMSampleBuffer`).
    public static func avcc(_ nals: [ArraySlice<UInt8>]) -> [UInt8] {
        nals.reduce(into: [UInt8]()) { result, nal in
            result += bigEndianBytes(UInt32(nal.count))
            result += nal
        }
    }

    public static func parseAccessUnit(_ annexB: [UInt8]) -> H264AccessUnit {
        let nals = nalUnits(in: annexB)
        let slices = nals.filter { isSlice($0) }
        return H264AccessUnit(
            sps: nals.first { type(of: $0) == spsType }.map { Array($0) },
            pps: nals.first { type(of: $0) == ppsType }.map { Array($0) },
            isKeyframe: slices.contains { type(of: $0) == idrType },
            avcc: avcc(slices)
        )
    }

    /// Separa um fluxo Annex-B em access units, cada um começando num AUD.
    /// Um AUD sem fatia (duplicado) é juntado ao access unit seguinte.
    public static func accessUnits(in stream: [UInt8]) -> [[UInt8]] {
        var starts: [Int] = []
        var index = 0
        while index + 3 < stream.count {
            if stream[index] == 0, stream[index + 1] == 0, stream[index + 2] == 1, stream[index + 3] & 0x1F == audType {
                starts.append(index > 0 && stream[index - 1] == 0 ? index - 1 : index)
                index += 4
            } else {
                index += 1
            }
        }
        var units: [[UInt8]] = []
        var pending: [UInt8] = []
        for (position, start) in starts.enumerated() {
            let end = position + 1 < starts.count ? starts[position + 1] : stream.count
            pending += stream[start..<end]
            if nalUnits(in: pending).contains(where: isSlice) {
                units.append(pending)
                pending = []
            }
        }
        if !pending.isEmpty, let last = units.popLast() { units.append(last + pending) }
        return units
    }

    private static func isSlice(_ nal: ArraySlice<UInt8>) -> Bool {
        let nalType = type(of: nal)
        return nalType == sliceType || nalType == idrType
    }

    private static func trimmingTrailingZeros(_ nal: ArraySlice<UInt8>) -> ArraySlice<UInt8> {
        var end = nal.endIndex
        while end > nal.startIndex, nal[end - 1] == 0 { end -= 1 }
        return nal[nal.startIndex..<end]
    }
}

public struct H264AccessUnit: Equatable, Sendable {
    public let sps: [UInt8]?
    public let pps: [UInt8]?
    public let isKeyframe: Bool
    /// Só as fatias (tipos 1 e 5), já em AVCC.
    public let avcc: [UInt8]
}
