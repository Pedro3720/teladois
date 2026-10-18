import XCTest
import UIKit

@MainActor
final class RestScreenUITests: XCTestCase {
    override func setUp() async throws {
        continueAfterFailure = false
    }

    /// Abre o app com hora, fuso, temperatura e bateria fixos e espera a paisagem.
    private func launch(dimSeconds: Int = 600) -> XCUIApplication {
        let app = XCUIApplication()
        app.launchArguments += [
            "-TDFixedDate", "2026-10-14T10:51:05-03:00",
            "-TDTimeZone", "America/Sao_Paulo",
            "-TDFixedTemp", "23",
            "-TDFixedBattery", "0.8",
            "-TDDimSeconds", String(dimSeconds),
        ]
        app.launch()
        XCUIDevice.shared.orientation = .landscapeLeft
        let window = app.windows.firstMatch
        XCTAssertTrue(window.waitForExistence(timeout: 5))
        let landscape = NSPredicate { _, _ in window.frame.width > window.frame.height }
        wait(for: [XCTNSPredicateExpectation(predicate: landscape, object: nil)], timeout: 10)
        XCTAssertTrue(app.staticTexts["clock"].waitForExistence(timeout: 5))
        return app
    }

    private func element(_ app: XCUIApplication, _ id: String) -> XCUIElement {
        app.descendants(matching: .any)[id].firstMatch
    }

    private func attachScreenshot(_ name: String) {
        let image = Self.normalized(XCUIScreen.main.screenshot().image)
        XCTAssertGreaterThan(image.size.width, image.size.height, "print deve ser em paisagem")
        let shot = XCTAttachment(image: image)
        shot.name = name
        shot.lifetime = .keepAlways
        add(shot)
    }

    func testClockPageShowsTimeDateTemperatureAndBattery() {
        let app = launch()
        XCTAssertEqual(app.staticTexts["clock"].label, "10:51")
        XCTAssertEqual(element(app, "date").label, "QUA., 14")
        XCTAssertEqual(element(app, "temp").label, "23°C")
        XCTAssertEqual(element(app, "battery").value as? String, "80%")
        attachScreenshot("01-relogio")
    }

    func testLayoutFitsAndKeepsRightColumnAwayFromCorner() {
        let app = launch()
        let window = app.windows.firstMatch.frame
        let clock = app.staticTexts["clock"].frame
        let date = element(app, "date").frame
        let battery = element(app, "battery").frame
        for frame in [clock, date, battery] {
            XCTAssertTrue(window.contains(frame), "\(frame) fora da janela \(window)")
        }
        XCTAssertLessThan(clock.maxX, date.minX, "relógio invade a coluna da direita")
        XCTAssertGreaterThanOrEqual(window.maxX - battery.maxX, 44, "bateria colada na borda direita")
        XCTAssertGreaterThanOrEqual(battery.minY - window.minY, 24, "bateria colada no topo")
        XCTAssertGreaterThan(clock.height, window.height * 0.5, "relógio deve ocupar boa parte da altura")
    }

    func testDimsAfterTimeoutAndOneTapWakes() {
        let app = launch(dimSeconds: 4)
        let overlay = app.buttons["dim-overlay"]
        XCTAssertTrue(overlay.waitForExistence(timeout: 10), "tela deveria escurecer após 4 s sem toque")
        // A camada preta entra com fade de 1,2 s: espera a tela ficar preta de verdade antes do print.
        let black = XCTNSPredicateExpectation(
            predicate: NSPredicate { _, _ in Self.maxBrightness(XCUIScreen.main.screenshot().image) < 0.05 },
            object: nil
        )
        wait(for: [black], timeout: 5)
        attachScreenshot("02-escurecido")

        overlay.tap()
        let gone = XCTNSPredicateExpectation(predicate: NSPredicate(format: "exists == false"), object: overlay)
        wait(for: [gone], timeout: 3)
        XCTAssertTrue(app.staticTexts["clock"].isHittable, "relógio deveria voltar a aparecer")
        attachScreenshot("03-acordado")
    }
}
