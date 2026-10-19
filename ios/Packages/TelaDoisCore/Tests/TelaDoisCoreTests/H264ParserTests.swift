import XCTest
@testable import TelaDoisCore

final class H264ParserTests: XCTestCase {
    private func fixture() throws -> [UInt8] {
        let url = try XCTUnwrap(Bundle.module.url(forResource: "bars-64x64", withExtension: "h264", subdirectory: "Fixtures"))
        return try [UInt8](Data(contentsOf: url))
    }

    func testSplitsThreeAndFourByteStartCodes() {
        let stream: [UInt8] = [0, 0, 0, 1, 0x09, 0xF0, 0, 0, 1, 0x65, 0xAA, 0xBB]
        XCTAssertEqual(H264Parser.nalUnits(in: stream), [
            H264NALUnit(type: 9, bytes: [0x09, 0xF0]),
            H264NALUnit(type: 5, bytes: [0x65, 0xAA, 0xBB]),
        ])
    }

    func testFixtureNalTypes() throws {
        XCTAssertEqual(H264Parser.nalUnits(in: try fixture()).map(\.type), [9, 7, 8, 6, 5, 9, 1, 9, 1, 9, 1])
    }

    func testAccessUnitsSkipEmptyOnesAndKeepOnlyFirstAsKeyframe() throws {
        // AUD duplicado (como o h264_mf gera) não vira imagem vazia.
        let doubledAud = [UInt8]([0, 0, 0, 1, 0x09, 0xF0]) + (try fixture())
        let units = H264Parser.accessUnits(in: doubledAud)
        XCTAssertEqual(units.count, 4)
        XCTAssertEqual(units.map { H264Parser.containsIDR(H264Parser.nalUnits(in: $0)) }, [true, false, false, false])
    }

    func testAvccDropsParameterSetsAndPrefixesLengths() throws {
        let first = H264Parser.nalUnits(in: H264Parser.accessUnits(in: try fixture())[0])
        let avcc = H264Parser.avcc(first)
        let kept = first.filter { ![H264NALType.aud, H264NALType.sps, H264NALType.pps].contains($0.type) }
        XCTAssertEqual(kept.map(\.type), [6, 5])
        XCTAssertEqual(avcc.count, kept.reduce(0) { $0 + 4 + $1.bytes.count })
        let firstLength = Int(avcc[0]) << 24 | Int(avcc[1]) << 16 | Int(avcc[2]) << 8 | Int(avcc[3])
        XCTAssertEqual(firstLength, kept[0].bytes.count)
    }
}
