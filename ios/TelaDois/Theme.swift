import SwiftUI

/// Tokens de `docs/design/tokens.md`.
enum Theme {
    static let clock = Color(red: 0x7F / 255, green: 0xA9 / 255, blue: 0xF2 / 255)
    static let ink = Color(red: 0xF5 / 255, green: 0xF5 / 255, blue: 0xF7 / 255)
    static let muted = Color(red: 0x8E / 255, green: 0x8E / 255, blue: 0x93 / 255)
    static let battery = Color(red: 0x34 / 255, green: 0xC7 / 255, blue: 0x59 / 255)
    static let track = Color(red: 0x2C / 255, green: 0x2C / 255, blue: 0x2E / 255)

    /// Folgas mínimas até as bordas (cantos arredondados), somadas à área segura.
    static let rightClearance: CGFloat = 48
    static let topClearance: CGFloat = 24
    static let sideMargin: CGFloat = 16
    static let bottomMargin: CGFloat = 12
}
