import XCTest
@testable import TelaDoisCore

final class DimStateTests: XCTestCase {
    private let t0 = Date(timeIntervalSince1970: 1_000_000)

    func testStaysAwakeBeforeTimeout() {
        var state = DimState(now: t0, timeout: 60)
        state.update(now: t0.addingTimeInterval(59.9))
        XCTAssertFalse(state.isDimmed)
    }

    func testDimsAtTimeout() {
        var state = DimState(now: t0, timeout: 60)
        state.update(now: t0.addingTimeInterval(60))
        XCTAssertTrue(state.isDimmed)
    }

    func testTouchWhileAwakeRestartsCountdownAndPassesThrough() {
        var state = DimState(now: t0, timeout: 60)
        XCTAssertEqual(state.touch(now: t0.addingTimeInterval(50)), .passThrough)
        state.update(now: t0.addingTimeInterval(100))
        XCTAssertFalse(state.isDimmed)
        state.update(now: t0.addingTimeInterval(110))
        XCTAssertTrue(state.isDimmed)
    }

    func testTouchWhileDimmedWakesIsConsumedAndRestartsCountdown() {
        var state = DimState(now: t0, timeout: 60)
        state.update(now: t0.addingTimeInterval(61))
        XCTAssertEqual(state.touch(now: t0.addingTimeInterval(70)), .wokeUp)
        XCTAssertFalse(state.isDimmed)
        state.update(now: t0.addingTimeInterval(129))
        XCTAssertFalse(state.isDimmed)
        state.update(now: t0.addingTimeInterval(130))
        XCTAssertTrue(state.isDimmed)
    }

    func testTimeoutHasMinimumOfOneSecond() {
        XCTAssertEqual(DimState(now: t0, timeout: 0).timeout, 1)
    }
}
