import XCTest
@testable import TelaDoisCore

final class LinkStatsMeterTests: XCTestCase {
    func testStartsEmpty() {
        var meter = LinkStatsMeter()
        XCTAssertEqual(meter.take(), .zero)
    }

    func testCountsFramesBytesAndFailures() {
        var meter = LinkStatsMeter()
        meter.addBytes(1_000)
        meter.addBytes(500)
        meter.frameReceived()
        meter.frameReceived()
        meter.frameReceived()
        meter.frameDropped()
        meter.rendererFailed()
        let stats = meter.take()
        XCTAssertEqual(stats.bytesReceived, 1_500)
        XCTAssertEqual(stats.framesReceived, 3)
        XCTAssertEqual(stats.framesDropped, 1)
        XCTAssertEqual(stats.rendererFailures, 1)
        XCTAssertEqual(stats.framesEnqueued, 0)
    }

    func testAveragesAndMaximumsOfTheDurations() {
        var meter = LinkStatsMeter()
        meter.frameEnqueued(receiveToEnqueueMicros: 1_000)
        meter.frameEnqueued(receiveToEnqueueMicros: 3_000)
        meter.mainLag(micros: 100)
        meter.mainLag(micros: 200)
        meter.mainLag(micros: 600)
        let stats = meter.take()
        XCTAssertEqual(stats.framesEnqueued, 2)
        XCTAssertEqual(stats.receiveToEnqueueAvgMicros, 2_000)
        XCTAssertEqual(stats.receiveToEnqueueMaxMicros, 3_000)
        XCTAssertEqual(stats.mainLagAvgMicros, 300)
        XCTAssertEqual(stats.mainLagMaxMicros, 600)
    }

    func testTakeStartsANewInterval() {
        var meter = LinkStatsMeter()
        meter.frameReceived()
        meter.mainLag(micros: 900)
        _ = meter.take()
        meter.mainLag(micros: 10)
        let stats = meter.take()
        XCTAssertEqual(stats.framesReceived, 0)
        XCTAssertEqual(stats.mainLagAvgMicros, 10)
        XCTAssertEqual(stats.mainLagMaxMicros, 10)
    }

    func testSaturatesInsteadOfOverflowing() {
        var meter = LinkStatsMeter()
        meter.addBytes(Int(UInt32.max))
        meter.addBytes(10)
        meter.mainLag(micros: UInt64(UInt32.max) + 5)
        let stats = meter.take()
        XCTAssertEqual(stats.bytesReceived, UInt32.max)
        XCTAssertEqual(stats.mainLagMaxMicros, UInt32.max)
        XCTAssertEqual(stats.mainLagAvgMicros, UInt32.max)
    }
}
