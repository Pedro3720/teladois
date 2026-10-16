import XCTest
import UIKit

final class LaunchUITests: XCTestCase {
    override func setUp() {
        continueAfterFailure = false
    }

    func testLaunchShowsGreetingInLandscape() {
        let app = XCUIApplication()
        app.launch()

        // Gira depois de lançar e espera a janela ficar em paisagem antes de capturar.
        XCUIDevice.shared.orientation = .landscapeLeft
        let window = app.windows.firstMatch
        XCTAssertTrue(window.waitForExistence(timeout: 5))
        let landscape = NSPredicate { _, _ in window.frame.width > window.frame.height }
        wait(for: [XCTNSPredicateExpectation(predicate: landscape, object: nil)], timeout: 10)

        let greeting = app.staticTexts["greeting"]
        XCTAssertTrue(greeting.waitForExistence(timeout: 5))
        XCTAssertEqual(greeting.label, "Olá, TelaDois")

        // Prova do layout: paisagem, texto horizontal, inteiro dentro da janela e centralizado.
        let w = window.frame
        let g = greeting.frame
        XCTAssertGreaterThan(w.width, w.height)
        XCTAssertGreaterThan(g.width, g.height, "texto deve ser horizontal")
        XCTAssertTrue(w.contains(g), "texto deve estar inteiro dentro da janela: \(g) em \(w)")
        XCTAssertEqual(g.midX, w.midX, accuracy: 3)
        XCTAssertEqual(g.midY, w.midY, accuracy: 3)

        // Captura (diagnóstico: três variantes + dados de orientação).
        let appShot = app.screenshot()
        let screenShot = XCUIScreen.main.screenshot()
        let normalized = Self.normalized(appShot.image)
        let diag = """
        deviceOrientation=\(XCUIDevice.shared.orientation.rawValue)
        window=\(w) greeting=\(g)
        app.screenshot size=\(appShot.image.size) scale=\(appShot.image.scale) orientation=\(appShot.image.imageOrientation.rawValue)
        screen.screenshot size=\(screenShot.image.size) scale=\(screenShot.image.scale) orientation=\(screenShot.image.imageOrientation.rawValue)
        normalized size=\(normalized.size) scale=\(normalized.scale)
        """
        print(diag)
        attach(text: diag, name: "00-diagnostico")
        attach(image: appShot.image, name: "01a-app-screenshot")
        attach(image: screenShot.image, name: "01b-screen-screenshot")
        attach(image: normalized, name: "01-launch")
    }

    /// Redesenha a imagem respeitando `imageOrientation` (o desenho aplica a rotação).
    private static func normalized(_ image: UIImage) -> UIImage {
        let format = UIGraphicsImageRendererFormat()
        format.scale = image.scale
        let renderer = UIGraphicsImageRenderer(size: image.size, format: format)
        return renderer.image { _ in image.draw(in: CGRect(origin: .zero, size: image.size)) }
    }

    private func attach(image: UIImage, name: String) {
        let a = XCTAttachment(image: image)
        a.name = name
        a.lifetime = .keepAlways
        add(a)
    }

    private func attach(text: String, name: String) {
        let a = XCTAttachment(string: text)
        a.name = name
        a.lifetime = .keepAlways
        add(a)
    }
}
