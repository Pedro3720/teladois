import AVFoundation
import CoreMedia
import TelaDoisCore
import UIKit

/// Mostra o H.264 do PC: monta `CMSampleBuffer`s (AVCC) e entrega ao renderizador da camada,
/// que decodifica por hardware. Descarta quadros até o primeiro keyframe e, se a decodificação
/// falhar, limpa e pede outro keyframe ao PC.
@MainActor
final class StreamPlayer {
    let layer = AVSampleBufferDisplayLayer()
    var onNeedKeyframe: (@MainActor () -> Void)?

    private var format: CMVideoFormatDescription?
    private var waitingForKeyframe = true

    init() {
        layer.videoGravity = .resizeAspect
        layer.backgroundColor = UIColor.black.cgColor
    }

    func reset() {
        layer.sampleBufferRenderer.flush(removingDisplayedImage: true, completionHandler: nil)
        format = nil
        waitingForKeyframe = true
    }

    func enqueue(_ annexB: [UInt8]) {
        let unit = H264.parseAccessUnit(annexB)
        if let sps = unit.sps, let pps = unit.pps, let newFormat = Self.makeFormat(sps: sps, pps: pps) {
            format = newFormat
        }
        let renderer = layer.sampleBufferRenderer
        if renderer.status == .failed || renderer.requiresFlushToResumeDecoding {
            renderer.flush()
            waitingForKeyframe = true
            onNeedKeyframe?()
        }
        if waitingForKeyframe {
            guard unit.isKeyframe else { return }
            waitingForKeyframe = false
        }
        guard let format, !unit.avcc.isEmpty, let sample = Self.makeSample(unit.avcc, format: format) else { return }
        renderer.enqueue(sample)
    }

    private static func makeFormat(sps: [UInt8], pps: [UInt8]) -> CMVideoFormatDescription? {
        var format: CMVideoFormatDescription?
        let status = sps.withUnsafeBufferPointer { spsBuffer in
            pps.withUnsafeBufferPointer { ppsBuffer in
                guard let spsBase = spsBuffer.baseAddress, let ppsBase = ppsBuffer.baseAddress else { return OSStatus(-1) }
                return CMVideoFormatDescriptionCreateFromH264ParameterSets(
                    allocator: kCFAllocatorDefault,
                    parameterSetCount: 2,
                    parameterSetPointers: [spsBase, ppsBase],
                    parameterSetSizes: [sps.count, pps.count],
                    nalUnitHeaderLength: 4,
                    formatDescriptionOut: &format
                )
            }
        }
        return status == noErr ? format : nil
    }

    private static func makeSample(_ avcc: [UInt8], format: CMVideoFormatDescription) -> CMSampleBuffer? {
        var block: CMBlockBuffer?
        guard CMBlockBufferCreateWithMemoryBlock(
            allocator: kCFAllocatorDefault, memoryBlock: nil, blockLength: avcc.count,
            blockAllocator: kCFAllocatorDefault, customBlockSource: nil, offsetToData: 0,
            dataLength: avcc.count, flags: 0, blockBufferOut: &block
        ) == noErr, let block else { return nil }
        let copied = avcc.withUnsafeBytes { bytes -> OSStatus in
            guard let base = bytes.baseAddress else { return OSStatus(-1) }
            return CMBlockBufferReplaceDataBytes(with: base, blockBuffer: block, offsetIntoDestination: 0, dataLength: avcc.count)
        }
        guard copied == noErr else { return nil }
        var sample: CMSampleBuffer?
        var size = avcc.count
        guard CMSampleBufferCreateReady(
            allocator: kCFAllocatorDefault, dataBuffer: block, formatDescription: format,
            sampleCount: 1, sampleTimingEntryCount: 0, sampleTimingArray: nil,
            sampleSizeEntryCount: 1, sampleSizeArray: &size, sampleBufferOut: &sample
        ) == noErr, let sample else { return nil }
        // Sem relógio de apresentação: mostra assim que decodificar (menor latência).
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
