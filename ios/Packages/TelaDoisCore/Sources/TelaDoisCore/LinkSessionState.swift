import Foundation

/// Lado do iPhone na conversa com o PC: responde `PING` com `PONG`, encerra no `BYE`
/// e considera a conexão morta após `timeout` segundos sem receber nada.
public struct LinkSessionState: Sendable {
    public enum Action: Equatable, Sendable {
        case send(LinkFrame)
        case close
    }

    public let timeout: TimeInterval
    public private(set) var lastHeard: Date

    public init(now: Date, timeout: TimeInterval = LinkProtocol.timeout) {
        self.timeout = timeout
        self.lastHeard = now
    }

    public mutating func receive(_ frame: LinkFrame, now: Date) -> [Action] {
        lastHeard = now
        switch frame.messageType {
        case .ping:
            return LinkProtocol.timestamp(of: frame).map { [.send(LinkProtocol.pong($0))] } ?? []
        case .bye:
            return [.close]
        default:
            return []
        }
    }

    public func isTimedOut(now: Date) -> Bool {
        now.timeIntervalSince(lastHeard) > timeout
    }
}
