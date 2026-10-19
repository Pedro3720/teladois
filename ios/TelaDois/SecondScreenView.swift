import AVFoundation
import SwiftUI
import UIKit

/// Vídeo do PC em tela cheia, aspecto preservado, fundo preto.
struct SecondScreenView: UIViewRepresentable {
    let renderer: VideoRenderer

    func makeUIView(context: Context) -> VideoLayerView {
        VideoLayerView(videoLayer: renderer.layer)
    }

    func updateUIView(_ uiView: VideoLayerView, context: Context) {}
}

final class VideoLayerView: UIView {
    private let videoLayer: AVSampleBufferDisplayLayer

    init(videoLayer: AVSampleBufferDisplayLayer) {
        self.videoLayer = videoLayer
        super.init(frame: .zero)
        backgroundColor = .black
        layer.addSublayer(videoLayer)
    }

    required init?(coder: NSCoder) { nil }

    override func layoutSubviews() {
        super.layoutSubviews()
        videoLayer.frame = bounds
    }
}
