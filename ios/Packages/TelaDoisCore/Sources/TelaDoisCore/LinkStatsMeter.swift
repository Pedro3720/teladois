import Foundation

/// Acumula os medidores do vídeo no iPhone durante um intervalo; `take()` devolve o `LinkStats` e começa outro.
/// Valores que passariam de `UInt32` ficam no máximo (o cabo nunca chega perto disso em 1 s).
public struct LinkStatsMeter: Sendable {
    private var framesReceived: UInt64 = 0
    private var framesEnqueued: UInt64 = 0
    private var framesDropped: UInt64 = 0
    private var bytesReceived: UInt64 = 0
    private var rendererFailures: UInt64 = 0
    private var mainLagTally = DurationTally()
    private var receiveToEnqueueTally = DurationTally()

    public init() {}

    /// Bytes que chegaram do PC (todas as mensagens).
    public mutating func addBytes(_ count: Int) {
        bytesReceived &+= UInt64(max(count, 0))
    }

    /// Um `VIDEO` completo separado do fluxo.
    public mutating func frameReceived() {
        framesReceived &+= 1
    }

    /// Uma imagem entregue ao decodificador, `micros` depois de separada do fluxo.
    public mutating func frameEnqueued(receiveToEnqueueMicros micros: UInt64) {
        framesEnqueued &+= 1
        receiveToEnqueueTally.add(micros)
    }

    /// Uma imagem jogada fora antes do decodificador (sem formato, esperando imagem completa).
    public mutating func frameDropped() {
        framesDropped &+= 1
    }

    /// O decodificador estava em `.failed` e foi limpo.
    public mutating func rendererFailed() {
        rendererFailures &+= 1
    }

    /// Quanto um bloco agendado na fila principal esperou para rodar.
    public mutating func mainLag(micros: UInt64) {
        mainLagTally.add(micros)
    }

    public mutating func take() -> LinkStats {
        let stats = LinkStats(
            framesReceived: Self.clamp(framesReceived),
            framesEnqueued: Self.clamp(framesEnqueued),
            framesDropped: Self.clamp(framesDropped),
            bytesReceived: Self.clamp(bytesReceived),
            mainLagAvgMicros: Self.clamp(mainLagTally.average),
            mainLagMaxMicros: Self.clamp(mainLagTally.maximum),
            receiveToEnqueueAvgMicros: Self.clamp(receiveToEnqueueTally.average),
            receiveToEnqueueMaxMicros: Self.clamp(receiveToEnqueueTally.maximum),
            rendererFailures: Self.clamp(rendererFailures)
        )
        self = LinkStatsMeter()
        return stats
    }

    private static func clamp(_ value: UInt64) -> UInt32 {
        UInt32(min(value, UInt64(UInt32.max)))
    }

    private struct DurationTally: Sendable {
        private var count: UInt64 = 0
        private var sum: UInt64 = 0
        private(set) var maximum: UInt64 = 0

        var average: UInt64 { count == 0 ? 0 : sum / count }

        mutating func add(_ micros: UInt64) {
            count &+= 1
            let (total, overflow) = sum.addingReportingOverflow(micros)
            sum = overflow ? UInt64.max : total
            maximum = max(maximum, micros)
        }
    }
}
