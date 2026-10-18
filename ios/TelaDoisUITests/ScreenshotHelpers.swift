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

    /// Maior brilho (0…1) entre os pixels de uma cópia reduzida da imagem; 0 = tudo preto.
    static func maxBrightness(_ image: UIImage) -> CGFloat {
        guard let cgImage = image.cgImage else { return 1 }
        let width = 64, height = 64
        var pixels = [UInt8](repeating: 0, count: width * height * 4)
        let drawn = pixels.withUnsafeMutableBytes { buffer -> Bool in
            guard let context = CGContext(
                data: buffer.baseAddress, width: width, height: height,
                bitsPerComponent: 8, bytesPerRow: width * 4,
                space: CGColorSpaceCreateDeviceRGB(),
                bitmapInfo: CGImageAlphaInfo.noneSkipLast.rawValue
            ) else { return false }
            context.draw(cgImage, in: CGRect(x: 0, y: 0, width: width, height: height))
            return true
        }
        guard drawn else { return 1 }
        var brightest: UInt8 = 0
        for index in stride(from: 0, to: pixels.count, by: 4) {
            brightest = max(brightest, pixels[index], pixels[index + 1], pixels[index + 2])
        }
        return CGFloat(brightest) / 255
    }
}
