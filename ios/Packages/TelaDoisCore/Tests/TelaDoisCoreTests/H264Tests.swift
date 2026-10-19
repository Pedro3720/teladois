import XCTest
@testable import TelaDoisCore

final class H264Tests: XCTestCase {
    // AUD, SPS, PPS, IDR com código de início de 4 bytes; depois AUD + fatia com 3 bytes.
    private let keyframe: [UInt8] = [0, 0, 0, 1, 0x09, 0xF0,
                                     0, 0, 0, 1, 0x67, 0x4D, 0x00, 0x1F,
                                     0, 0, 0, 1, 0x68, 0xEE, 0x3C, 0x80,
                                     0, 0, 0, 1, 0x65, 0x88, 0x84]
    private let delta: [UInt8] = [0, 0, 1, 0x09, 0xF0, 0, 0, 1, 0x41, 0x9A, 0x02]

    func testSplitsNalUnitsWithThreeAndFourByteStartCodes() {
        let types = H264.nalUnits(in: keyframe).map(H264.type(of:))
        XCTAssertEqual(types, [9, 7, 8, 5])
        XCTAssertEqual(Array(H264.nalUnits(in: keyframe)[1]), [0x67, 0x4D, 0x00, 0x1F])
    }

    func testKeyframeCarriesParameterSetsAndOnlySlicesInAvcc() {
        let unit = H264.parseAccessUnit(keyframe)
        XCTAssertTrue(unit.isKeyframe)
        XCTAssertEqual(unit.sps, [0x67, 0x4D, 0x00, 0x1F])
        XCTAssertEqual(unit.pps, [0x68, 0xEE, 0x3C, 0x80])
        XCTAssertEqual(unit.avcc, [0, 0, 0, 3, 0x65, 0x88, 0x84])
    }

    func testDeltaFrameHasNoParameterSets() {
        let unit = H264.parseAccessUnit(delta)
        XCTAssertFalse(unit.isKeyframe)
        XCTAssertNil(unit.sps)
        XCTAssertEqual(unit.avcc, [0, 0, 0, 3, 0x41, 0x9A, 0x02])
    }

    func testSplitsStreamIntoAccessUnitsAtEachAud() {
        let units = H264.accessUnits(in: keyframe + delta)
        XCTAssertEqual(units, [keyframe, delta])
    }

    func testAudWithoutSliceIsMergedIntoNextAccessUnit() {
        // O AMF manda AUD duplicado quando se pede outro: o AUD vazio não vira quadro.
        let lonelyAud: [UInt8] = [0, 0, 0, 1, 0x09, 0xF0]
        XCTAssertEqual(H264.accessUnits(in: lonelyAud + delta), [lonelyAud + delta])
    }
}
