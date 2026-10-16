import XCTest
@testable import TelaDoisCore

final class ClockFormatterTests: XCTestCase {
    private let saoPaulo = TimeZone(identifier: "America/Sao_Paulo")!

    private func date(_ iso: String) -> Date {
        ISO8601DateFormatter().date(from: iso)!
    }

    func testReferenceStandByTime() {
        let text = ClockFormatter.text(for: date("2026-10-14T10:51:30-03:00"), timeZone: saoPaulo)
        XCTAssertEqual(text, ClockText(hours: "10", minutes: "51", weekday: "QUA.", day: "14"))
    }

    func testUses24HoursAndPadsMinutes() {
        let text = ClockFormatter.text(for: date("2026-10-18T21:05:00-03:00"), timeZone: saoPaulo)
        XCTAssertEqual(text, ClockText(hours: "21", minutes: "05", weekday: "DOM.", day: "18"))
    }

    func testMidnightAndSingleDigitDay() {
        let text = ClockFormatter.text(for: date("2026-11-02T00:07:00-03:00"), timeZone: saoPaulo)
        XCTAssertEqual(text, ClockText(hours: "00", minutes: "07", weekday: "SEG.", day: "2"))
    }

    func testUsesGivenTimeZone() {
        let text = ClockFormatter.text(for: date("2026-10-14T10:51:30-03:00"), timeZone: TimeZone(identifier: "UTC")!)
        XCTAssertEqual(text.hours, "13")
    }
}
