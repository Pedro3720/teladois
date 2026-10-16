import Foundation

/// Estado do Modo Descanso: escurece depois de `timeout` segundos sem toque; um toque com a
/// tela preta só acorda (o toque é consumido e não chega aos controles embaixo).
public struct DimState: Equatable, Sendable {
    public enum TouchResult: Equatable, Sendable {
        case passThrough
        case wokeUp
    }

    public let timeout: TimeInterval
    public private(set) var isDimmed = false
    private var lastInteraction: Date

    public init(now: Date, timeout: TimeInterval) {
        self.timeout = max(1, timeout)
        self.lastInteraction = now
    }

    public mutating func update(now: Date) {
        if !isDimmed, now.timeIntervalSince(lastInteraction) >= timeout {
            isDimmed = true
        }
    }

    @discardableResult
    public mutating func touch(now: Date) -> TouchResult {
        lastInteraction = now
        if isDimmed {
            isDimmed = false
            return .wokeUp
        }
        return .passThrough
    }
}
