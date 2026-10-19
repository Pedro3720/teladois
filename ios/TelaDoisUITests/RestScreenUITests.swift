import XCTest
import UIKit
import TelaDoisCore

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

    func testPcConnectionShowsStatusAndAnswersPing() {
        let app = launch()
        XCTAssertFalse(element(app, "link-status").exists, "sem PC não deve aparecer o aviso")
        guard let pc = FakePC.connect() else { return XCTFail("o app não aceitou conexão em 127.0.0.1:7100") }

        let hello = pc.readFrame().flatMap(LinkProtocol.parseHello)
        XCTAssertEqual(hello?.version, LinkProtocol.version)
        XCTAssertGreaterThan(hello?.width ?? 0, hello?.height ?? 0, "HELLO deve vir em paisagem")
        XCTAssertTrue(element(app, "link-status").waitForExistence(timeout: 5), "deveria mostrar PC conectado")

        pc.send(LinkProtocol.ping(42))
        let pong = pc.readFrame()
        XCTAssertEqual(pong?.messageType, .pong)
        XCTAssertEqual(pong.flatMap(LinkProtocol.timestamp(of:)), 42)
        attachScreenshot("04-pc-conectado")

        pc.send(LinkProtocol.bye())
        XCTAssertTrue(element(app, "link-status").waitForNonExistence(timeout: 3), "BYE deveria desconectar")
    }

    private static func fixture() -> [UInt8] {
        let url = Bundle(for: FakePC.self).url(forResource: "testsrc", withExtension: "h264")!
        return [UInt8](try! Data(contentsOf: url))
    }

    /// Mantém a conexão viva (PING) por `seconds`, lendo o que o app mandar.
    private func keepAlive(_ pc: FakePC, seconds: TimeInterval) {
        let end = Date().addingTimeInterval(seconds)
        while Date() < end {
            pc.send(LinkProtocol.ping(1))
            _ = pc.readFrame()
            Thread.sleep(forTimeInterval: 0.5)
        }
    }

    func testVideoFromPcShowsSecondScreenAndKeepsScreenOn() {
        let app = launch(dimSeconds: 2)
        guard let pc = FakePC.connect() else { return XCTFail("o app não aceitou conexão em 127.0.0.1:7100") }
        XCTAssertNotNil(pc.readFrame().flatMap(LinkProtocol.parseHello))

        pc.send(LinkProtocol.config(width: 640, height: 296, fps: 30))
        for unit in H264.accessUnits(in: Self.fixture()) {
            pc.send(LinkProtocol.video(unit))
        }
        XCTAssertTrue(element(app, "second-screen").waitForExistence(timeout: 5), "deveria mostrar a Segunda Tela")

        // Mais tempo que o escurecer (2 s): com vídeo, a tela não pode apagar.
        keepAlive(pc, seconds: 3)
        XCTAssertFalse(app.buttons["dim-overlay"].exists, "com vídeo a tela não escurece")
        let lit = XCTNSPredicateExpectation(
            predicate: NSPredicate { _, _ in Self.maxBrightness(XCUIScreen.main.screenshot().image) > 0.3 },
            object: nil
        )
        wait(for: [lit], timeout: 5)
        attachScreenshot("05-segunda-tela")

        pc.send(LinkProtocol.bye())
        XCTAssertTrue(app.staticTexts["clock"].waitForExistence(timeout: 3), "BYE deveria voltar ao relógio")
    }

    func testSilentPcIsDroppedAfterTimeout() {
        let app = launch()
        guard let pc = FakePC.connect() else { return XCTFail("o app não aceitou conexão em 127.0.0.1:7100") }
        XCTAssertNotNil(pc.readFrame().flatMap(LinkProtocol.parseHello))
        XCTAssertTrue(element(app, "link-status").waitForExistence(timeout: 5))
        // Sem PING por mais de 3 s, o app fecha a conexão (recv devolve 0 antes dos 5 s do teste).
        XCTAssertNil(pc.readFrame(), "o app deveria fechar a conexão de um PC calado")
        XCTAssertTrue(element(app, "link-status").waitForNonExistence(timeout: 2))
    }
}
