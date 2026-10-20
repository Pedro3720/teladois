import XCTest
@testable import TelaDoisCore

final class LinkProtocolTests: XCTestCase {
    private func hex(_ string: String) -> [UInt8] {
        stride(from: 0, to: string.count, by: 2).map { offset in
            let start = string.index(string.startIndex, offsetBy: offset)
            return UInt8(string[start..<string.index(start, offsetBy: 2)], radix: 16)!
        }
    }

    func testHelloMatchesGoldenBytes() {
        XCTAssertEqual(LinkProtocol.hello(width: 2556, height: 1179).encoded(), hex("010000000A0001000009FC0000049B"))
    }

    func testPingMatchesGoldenBytes() {
        XCTAssertEqual(LinkProtocol.ping(1_234_567_890_123).encoded(), hex("05000000080000011F71FB04CB"))
    }

    func testConfigMatchesGoldenBytes() {
        XCTAssertEqual(LinkProtocol.config(width: 1366, height: 768, fps: 60).encoded(), hex("020000000A0000055600000300003C"))
    }

    func testVideoMatchesGoldenBytes() {
        XCTAssertEqual(
            LinkProtocol.video(keyframe: true, timestamp: 1000, accessUnit: hex("0000000109F0")).encoded(),
            hex("030000000F0100000000000003E80000000109F0")
        )
    }

    func testVideoEndMatchesGoldenBytes() {
        XCTAssertEqual(LinkProtocol.videoEnd().encoded(), hex("0800000000"))
        XCTAssertEqual(LinkFrame(type: 8).messageType, .videoEnd)
    }

    func testStatsMatchesGoldenBytes() {
        let stats = LinkStats(
            framesReceived: 1, framesEnqueued: 2, framesDropped: 3, bytesReceived: 4,
            mainLagAvgMicros: 5, mainLagMaxMicros: 6, receiveToEnqueueAvgMicros: 7, receiveToEnqueueMaxMicros: 8,
            rendererFailures: 9
        )
        XCTAssertEqual(
            LinkProtocol.stats(stats).encoded(),
            hex("0900000024000000010000000200000003000000040000000500000006000000070000000800000009")
        )
        XCTAssertEqual(LinkFrame(type: 9).messageType, .stats)
    }

    func testParsesStats() {
        let stats = LinkStats(
            framesReceived: 60, framesEnqueued: 59, framesDropped: 1, bytesReceived: 400_000,
            mainLagAvgMicros: 2_000, mainLagMaxMicros: 40_000, receiveToEnqueueAvgMicros: 1_000, receiveToEnqueueMaxMicros: 9_000,
            rendererFailures: 0
        )
        XCTAssertEqual(LinkProtocol.parseStats(LinkProtocol.stats(stats)), stats)
    }

    func testRejectsStatsWithWrongSize() {
        XCTAssertNil(LinkProtocol.parseStats(LinkFrame(.stats, payload: [UInt8](repeating: 0, count: 35))))
        XCTAssertNil(LinkProtocol.parseStats(LinkFrame(.stats, payload: [UInt8](repeating: 0, count: 37))))
        XCTAssertNil(LinkProtocol.parseStats(LinkFrame(.pong, payload: [UInt8](repeating: 0, count: 36))))
    }

    func testMessageTypesKeepTheBytesOfTheCSharpSide() {
        let types: [LinkMessageType] = [.hello, .config, .video, .keyframeRequest, .ping, .pong, .bye, .videoEnd, .stats]
        XCTAssertEqual(types.map(\.rawValue), [1, 2, 3, 4, 5, 6, 7, 8, 9])
    }

    func testParsesConfigAndVideo() {
        XCTAssertEqual(
            LinkProtocol.parseConfig(LinkProtocol.config(width: 2556, height: 1179, fps: 30)),
            LinkVideoConfig(width: 2556, height: 1179, fps: 30)
        )
        XCTAssertEqual(
            LinkProtocol.parseVideo(LinkProtocol.video(keyframe: false, timestamp: 42, accessUnit: [1, 2, 3])),
            LinkVideoFrame(isKeyframe: false, timestamp: 42, accessUnit: [1, 2, 3])
        )
    }

    func testRejectsMalformedConfigAndVideo() {
        XCTAssertNil(LinkProtocol.parseConfig(LinkFrame(.config, payload: [0, 1])))
        XCTAssertNil(LinkProtocol.parseVideo(LinkFrame(.video, payload: [1, 0, 0])))
        XCTAssertNil(LinkProtocol.parseVideo(LinkProtocol.ping(1)))
    }

    func testParsesHello() {
        XCTAssertEqual(
            LinkProtocol.parseHello(LinkProtocol.hello(width: 2622, height: 1206)),
            LinkHello(version: 1, width: 2622, height: 1206)
        )
    }

    func testRejectsHelloWithWrongSize() {
        XCTAssertNil(LinkProtocol.parseHello(LinkFrame(.hello, payload: [0, 1])))
    }

    func testPongCarriesTimestamp() {
        XCTAssertEqual(LinkProtocol.timestamp(of: LinkProtocol.pong(42)), 42)
        XCTAssertNil(LinkProtocol.timestamp(of: LinkProtocol.bye()))
    }

    func testDecoderHandlesSplitAndBatchedFrames() throws {
        let bytes = LinkProtocol.hello(width: 2556, height: 1179).encoded() + LinkProtocol.ping(7).encoded()
        var decoder = LinkFrameDecoder()
        let first = try decoder.append(bytes[0..<3])
        XCTAssertEqual(first, [])
        let rest = try decoder.append(bytes[3...])
        XCTAssertEqual(rest.map(\.messageType), [.hello, .ping])
        let none = try decoder.append([UInt8]())
        XCTAssertEqual(none, [])
    }

    func testDecoderRejectsHugePayload() {
        var decoder = LinkFrameDecoder()
        XCTAssertThrowsError(try decoder.append(hex("03FFFFFFFF")))
    }

    func testUnknownTypeHasNoMessageType() {
        XCTAssertNil(LinkFrame(type: 99).messageType)
    }
}
