import XCTest
@testable import TelaDoisCore

final class OpenMeteoTests: XCTestCase {
    func testBuildsCurrentTemperatureURLWithThreeDecimals() {
        let url = OpenMeteo.currentTemperatureURL(latitude: -23.55052, longitude: -46.633308)
        XCTAssertEqual(
            url.absoluteString,
            "https://api.open-meteo.com/v1/forecast?latitude=-23.551&longitude=-46.633&current=temperature_2m"
        )
    }

    func testParsesCurrentTemperature() throws {
        let json = #"{"latitude":-23.5,"current_units":{"temperature_2m":"°C"},"current":{"time":"2026-10-14T10:45","interval":900,"temperature_2m":23.4}}"#
        XCTAssertEqual(try OpenMeteo.parseCurrentTemperature(Data(json.utf8)), 23.4, accuracy: 0.001)
    }

    func testRejectsResponseWithoutTemperature() {
        let json = #"{"error":true,"reason":"Latitude must be in range of -90 to 90°."}"#
        XCTAssertThrowsError(try OpenMeteo.parseCurrentTemperature(Data(json.utf8)))
    }

    func testLabelRoundsToWholeDegrees() {
        XCTAssertEqual(OpenMeteo.label(celsius: 23.4), "23°C")
        XCTAssertEqual(OpenMeteo.label(celsius: 22.5), "23°C")
        XCTAssertEqual(OpenMeteo.label(celsius: -0.4), "0°C")
        XCTAssertEqual(OpenMeteo.label(celsius: -3.6), "-4°C")
    }
}
