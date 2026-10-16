import XCTest

final class LaunchUITests: XCTestCase {
    override func setUp() {
        continueAfterFailure = false
        XCUIDevice.shared.orientation = .landscapeLeft
    }

    func testLaunchShowsGreetingInLandscape() {
        let app = XCUIApplication()
        app.launch()

        let greeting = app.staticTexts["greeting"]
        XCTAssertTrue(greeting.waitForExistence(timeout: 5))
        XCTAssertEqual(greeting.label, "Olá, TelaDois")
        XCTAssertGreaterThan(app.windows.firstMatch.frame.width, app.windows.firstMatch.frame.height)

        let shot = XCTAttachment(screenshot: app.screenshot())
        shot.name = "01-launch"
        shot.lifetime = .keepAlways
        add(shot)
    }
}
