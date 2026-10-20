import AVFoundation
import TelaDoisCore
import UIKit

/// Decodifica as imagens H.264 do PC no hardware do iPhone (`AVSampleBufferDisplayLayer`).
/// Trabalha numa fila própria, fora da thread da interface. Em erro, limpa e espera a próxima imagem completa.
final class VideoRenderer: @unchecked Sendable {
    let layer = AVSampleBufferDisplayLayer()
    /// Medidores (diagnóstico): na tela, descartadas, falhas e o tempo de separar a mensagem até entregar ao decodificador.
    let stats = VideoStats()
    private let queue = DispatchQueue(label: "teladois.video")
    // Só acessados na `queue`.
    private var format: CMVideoFormatDescription?
    private var waitingForKeyframe = true

    init() {
        layer.videoGravity = .resizeAspect
        layer.backgroundColor = UIColor.black.cgColor
    }

    /// `receivedAt`: `VideoStats.now()` de quando a mensagem foi separada do fluxo.
    func enqueue(_ frame: LinkVideoFrame, receivedAt: UInt64) {
        queue.async { self.handle(frame, receivedAt: receivedAt) }
    }

    func reset() {
        queue.async {
            self.format = nil
            self.waitingForKeyframe = true
            // Tira também a imagem na tela: ao religar, não aparece a última imagem da vez anterior.
            self.layer.sampleBufferRenderer.flush(removingDisplayedImage: true, completionHandler: nil)
        }
    }

    private func handle(_ frame: LinkVideoFrame, receivedAt: UInt64) {
        let renderer = layer.sampleBufferRenderer
        if renderer.status == .failed {
            renderer.flush()
            waitingForKeyframe = true
            stats.record { $0.rendererFailed() }
        }
        let nals = H264Parser.nalUnits(in: frame.accessUnit)
        if let sps = nals.first(where: { $0.type == H264NALType.sps }),
           let pps = nals.first(where: { $0.type == H264NALType.pps }) {
            format = Self.makeFormat(sps: sps.bytes, pps: pps.bytes) ?? format
        }
        if waitingForKeyframe {
            guard frame.isKeyframe, H264Parser.containsIDR(nals) else {
                stats.record { $0.frameDropped() }
                return
            }
            waitingForKeyframe = false
        }
        guard let format, let sample = Self.makeSample(H264Parser.avcc(nals), format: format) else {
            stats.record { $0.frameDropped() }
            return
        }
        renderer.enqueue(sample)
        let micros = VideoStats.micros(since: receivedAt)
        stats.record { $0.frameEnqueued(receiveToEnqueueMicros: micros) }
    }

    private static func makeFormat(sps: [UInt8], pps: [UInt8]) -> CMVideoFormatDescription? {
        var format: CMVideoFormatDescription?
        let status = sps.withUnsafeBufferPointer { spsBuffer in
            pps.withUnsafeBufferPointer { ppsBuffer -> OSStatus in
                guard let spsBase = spsBuffer.baseAddress, let ppsBase = ppsBuffer.baseAddress else { return -1 }
                let pointers = [spsBase, ppsBase]
                let sizes = [sps.count, pps.count]
                return CMVideoFormatDescriptionCreateFromH264ParameterSets(
                    allocator: kCFAllocatorDefault,
                    parameterSetCount: 2,
                    parameterSetPointers: pointers,
                    parameterSetSizes: sizes,
                    nalUnitHeaderLength: 4,
                    formatDescriptionOut: &format
                )
            }
        }
        return status == noErr ? format : nil
    }

    private static func makeSample(_ avcc: [UInt8], format: CMVideoFormatDescription) -> CMSampleBuffer? {
        guard !avcc.isEmpty else { return nil }
        var block: CMBlockBuffer?
        guard CMBlockBufferCreateWithMemoryBlock(
            allocator: kCFAllocatorDefault, memoryBlock: nil, blockLength: avcc.count,
            blockAllocator: kCFAllocatorDefault, customBlockSource: nil, offsetToData: 0,
            dataLength: avcc.count, flags: 0, blockBufferOut: &block
        ) == kCMBlockBufferNoErr, let block else { return nil }
        let copied = avcc.withUnsafeBytes { bytes -> OSStatus in
            guard let base = bytes.baseAddress else { return -1 }
            return CMBlockBufferReplaceDataBytes(with: base, blockBuffer: block, offsetIntoDestination: 0, dataLength: avcc.count)
        }
        guard copied == kCMBlockBufferNoErr else { return nil }
        var sample: CMSampleBuffer?
        var size = avcc.count
        guard CMSampleBufferCreateReady(
            allocator: kCFAllocatorDefault, dataBuffer: block, formatDescription: format,
            sampleCount: 1, sampleTimingEntryCount: 0, sampleTimingArray: nil,
            sampleSizeEntryCount: 1, sampleSizeArray: &size, sampleBufferOut: &sample
        ) == noErr, let sample else { return nil }
        if let attachments = CMSampleBufferGetSampleAttachmentsArray(sample, createIfNecessary: true),
           CFArrayGetCount(attachments) > 0 {
            let dictionary = unsafeBitCast(CFArrayGetValueAtIndex(attachments, 0), to: CFMutableDictionary.self)
            CFDictionarySetValue(
                dictionary,
                Unmanaged.passUnretained(kCMSampleAttachmentKey_DisplayImmediately).toOpaque(),
                Unmanaged.passUnretained(kCFBooleanTrue).toOpaque()
            )
        }
        return sample
    }
}
