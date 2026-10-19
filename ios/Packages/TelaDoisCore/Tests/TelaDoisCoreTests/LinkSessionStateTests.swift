import XCTest
@testable import TelaDoisCore

final class LinkSessionStateTests: XCTestCase {
    private let t0 = Date(timeIntervalSince1970: 1_000_000)

    func testPingIsAnsweredWithPongCarryingSameTimestamp() {
        var state = LinkSessionState(now: t0)
        XCTAssertEqual(state.receive(LinkProtocol.ping(99), now: t0), [.send(LinkProtocol.pong(99))])
    }

    func testByeClosesTheSession() {
        var state = LinkSessionState(now: t0)
        XCTAssertEqual(state.receive(LinkProtocol.bye(), now: t0), [.close])
    }

    func testOtherFramesNeedNoReply() {
        var state = LinkSessionState(now: t0)
        XCTAssertEqual(state.receive(LinkFrame(type: 99), now: t0), [])
    }

    func testTimesOutAfterThreeSecondsOfSilence() {
        let state = LinkSessionState(now: t0)
        XCTAssertFalse(state.isTimedOut(now: t0.addingTimeInterval(3)))
        XCTAssertTrue(state.isTimedOut(now: t0.addingTimeInterval(3.01)))
    }

    func testAnyFrameRefreshesTheTimeout() {
        var state = LinkSessionState(now: t0)
        _ = state.receive(LinkFrame(type: 99), now: t0.addingTimeInterval(2))
        XCTAssertFalse(state.isTimedOut(now: t0.addingTimeInterval(4.5)))
    }
}
