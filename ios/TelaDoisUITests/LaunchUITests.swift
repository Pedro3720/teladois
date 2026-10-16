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

        // XCUIScreen entrega o buffer do framebuffer (retrato) com `imageOrientation` correto;
        // o export do .xcresult ignora essa orientação. Redesenhar aplica a rotação nos pixels.
        let image = Self.normalized(XCUIScreen.main.screenshot().image)
        XCTAssertGreaterThan(image.size.width, image.size.height, "print deve ser em paisagem")

        let shot = XCTAttachment(image: image)
        shot.name = "01-launch"
        shot.lifetime = .keepAlways
        add(shot)
    }

    /// Redesenha a imagem respeitando `imageOrientation` (o desenho aplica a rotação).
    private static func normalized(_ image: UIImage) -> UIImage {
        let format = UIGraphicsImageRendererFormat()
        format.scale = image.scale
        let renderer = UIGraphicsImageRenderer(size: image.size, format: format)
        return renderer.image { _ in image.draw(in: CGRect(origin: .zero, size: image.size)) }
    }
}
