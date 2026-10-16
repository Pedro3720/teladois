import UIKit

/// Guarda o brilho do usuário ao escurecer e devolve ao acordar ou ao sair do app.
@MainActor
final class ScreenBrightness {
    private var saved: CGFloat?

    private var screen: UIScreen? {
        UIApplication.shared.connectedScenes.compactMap { ($0 as? UIWindowScene)?.screen }.first
    }

    func dim() {
        guard saved == nil, let screen else { return }
        saved = screen.brightness
        screen.brightness = 0
    }

    func restore() {
        guard let value = saved, let screen else { return }
        screen.brightness = value
        saved = nil
    }
}
