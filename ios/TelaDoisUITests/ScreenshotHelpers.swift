import XCTest
import UIKit

extension XCTestCase {
    /// Redesenha a imagem respeitando `imageOrientation` (o desenho aplica a rotação nos pixels).
    ///
    /// `XCUIScreen.main.screenshot()` entrega o buffer do framebuffer (retrato) com `imageOrientation`
    /// correto; o export do .xcresult ignora essa orientação, então normalize antes de anexar.
    @MainActor
    static func normalized(_ image: UIImage) -> UIImage {
        let format = UIGraphicsImageRendererFormat()
        format.scale = image.scale
        let renderer = UIGraphicsImageRenderer(size: image.size, format: format)
        return renderer.image { _ in image.draw(in: CGRect(origin: .zero, size: image.size)) }
    }
}
