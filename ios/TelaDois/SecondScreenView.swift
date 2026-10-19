import SwiftUI
import UIKit

/// Tela cheia com o vídeo do PC.
struct SecondScreenView: UIViewRepresentable {
    let player: StreamPlayer

    func makeUIView(context: Context) -> PlayerView {
        let view = PlayerView()
        view.backgroundColor = .black
        view.layer.addSublayer(player.layer)
        view.isAccessibilityElement = true
        view.accessibilityIdentifier = "second-screen"
        view.accessibilityLabel = "Segunda tela do PC"
        return view
    }

    func updateUIView(_ uiView: PlayerView, context: Context) {}

    final class PlayerView: UIView {
        override func layoutSubviews() {
            super.layoutSubviews()
            layer.sublayers?.forEach { $0.frame = bounds }
        }
    }
}
