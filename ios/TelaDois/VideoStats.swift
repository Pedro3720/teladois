import Dispatch
import os
import TelaDoisCore

/// Medidores da Segunda Tela no iPhone (só diagnóstico): somados na fila principal, na fila do vídeo e no relógio da
/// fila principal; o `UsbLink` manda o intervalo ao PC num `STATS` por segundo.
final class VideoStats: Sendable {
    private let meter = OSAllocatedUnfairLock(initialState: LinkStatsMeter())

    func record(_ change: @Sendable (inout LinkStatsMeter) -> Void) {
        meter.withLock { change(&$0) }
    }

    /// Devolve o intervalo e começa outro.
    func take() -> LinkStats {
        meter.withLock { $0.take() }
    }

    /// Relógio monotônico em ns (`DispatchTime`), para medir durações entre filas.
    static func now() -> UInt64 {
        DispatchTime.now().uptimeNanoseconds
    }

    static func micros(since start: UInt64) -> UInt64 {
        let current = now()
        return current > start ? (current - start) / 1_000 : 0
    }
}

/// Mede o atraso da fila principal: a cada 16 ms, numa fila de fundo, agenda um bloco na principal e anota quanto ele
/// esperou para rodar. Ligado só com a Segunda Tela ativa.
@MainActor
final class MainLagProbe {
    private let stats: VideoStats
    private var timer: DispatchSourceTimer?

    init(stats: VideoStats) {
        self.stats = stats
    }

    func start() {
        guard timer == nil else { return }
        let timer = Self.makeTimer(stats: stats)
        timer.resume()
        self.timer = timer
    }

    func stop() {
        timer?.cancel()
        timer = nil
    }

    /// Fora do ator principal: o bloco do relógio roda na fila de fundo.
    private nonisolated static func makeTimer(stats: VideoStats) -> DispatchSourceTimer {
        let timer = DispatchSource.makeTimerSource(queue: DispatchQueue(label: "teladois.main-lag", qos: .userInitiated))
        timer.schedule(deadline: .now(), repeating: .milliseconds(16), leeway: .milliseconds(1))
        timer.setEventHandler {
            let scheduled = VideoStats.now()
            DispatchQueue.main.async {
                let lag = VideoStats.micros(since: scheduled)
                stats.record { $0.mainLag(micros: lag) }
            }
        }
        return timer
    }
}
