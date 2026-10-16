import XCTest
@testable import TelaDois

final class GreetingTests: XCTestCase {
    func testGreetingText() {
        XCTAssertEqual(Greeting.text, "Olá, TelaDois")
    }
}
