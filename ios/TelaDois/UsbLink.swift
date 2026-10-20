import Foundation
import Network
import Observation
import TelaDoisCore
import UIKit

/// Escuta o PC pelo cabo: o usbmuxd entrega o túnel em 127.0.0.1:7100 no iPhone.
/// Manda HELLO, responde PING com PONG e fecha no BYE, em erro ou após 3 s sem notícias do PC.
@MainActor
@Observable
final class UsbLink {
    private(set) var isConnected = false
    /// Formato do vídeo do PC; `nil` = sem Segunda Tela (Modo Descanso).
    private(set) var videoConfig: LinkVideoConfig?
    /// Constante: o `@Observable` não acompanha `let`.
    let renderer = VideoRenderer()

    @ObservationIgnored private var listener: NWListener?
    @ObservationIgnored private var connection: NWConnection?
    @ObservationIgnored private var decoder = LinkFrameDecoder()
    @ObservationIgnored private var session = LinkSessionState(now: Date())
    @ObservationIgnored private var watchdog: Task<Void, Never>?

    func start() {
        guard listener == nil, let port = NWEndpoint.Port(rawValue: LinkProtocol.port) else { return }
        let parameters = NWParameters.tcp
        parameters.allowLocalEndpointReuse = true
        // Só loopback: nada exposto no Wi-Fi e sem pedido de "rede local".
        parameters.requiredLocalEndpoint = .hostPort(host: .ipv4(.loopback), port: port)
        guard let listener = try? NWListener(using: parameters) else { return }
        listener.newConnectionHandler = { [weak self] newConnection in
            MainActor.assumeIsolated { self?.accept(newConnection) }
        }
        listener.stateUpdateHandler = { [weak self] state in
            guard case .failed = state else { return }
            MainActor.assumeIsolated {
                self?.listener?.cancel()
                self?.listener = nil
            }
        }
        listener.start(queue: .main)
        self.listener = listener
    }

    /// App foi para o fundo: o PC percebe o socket fechado e encerra a sessão.
    func stop() {
        disconnect()
        listener?.cancel()
        listener = nil
    }

    private func accept(_ newConnection: NWConnection) {
        disconnect()
        connection = newConnection
        decoder = LinkFrameDecoder()
        let id = ObjectIdentifier(newConnection)
        newConnection.stateUpdateHandler = { [weak self] state in
            MainActor.assumeIsolated {
                guard let self, self.isCurrent(id) else { return }
                switch state {
                case .ready: self.didConnect(id)
                case .failed, .cancelled: self.disconnect()
                default: break
                }
            }
        }
        newConnection.start(queue: .main)
    }

    private func isCurrent(_ id: ObjectIdentifier) -> Bool {
        connection.map { ObjectIdentifier($0) == id } ?? false
    }

    private func didConnect(_ id: ObjectIdentifier) {
        let size = Self.landscapePixels()
        session = LinkSessionState(now: Date())
        send(LinkProtocol.hello(width: size.width, height: size.height))
        isConnected = true
        receiveNext(id)
        watchdog = Task { [weak self] in
            while !Task.isCancelled {
                try? await Task.sleep(for: .milliseconds(500))
                guard let self, self.isCurrent(id) else { return }
                if self.session.isTimedOut(now: Date()) {
                    self.disconnect()
                    return
                }
            }
        }
    }

    private func receiveNext(_ id: ObjectIdentifier) {
        connection?.receive(minimumIncompleteLength: 1, maximumLength: 1024 * 1024) { [weak self] data, _, isComplete, error in
            MainActor.assumeIsolated {
                guard let self, self.isCurrent(id) else { return }
                if let data, !data.isEmpty, !self.handle(data) { return }
                if isComplete || error != nil {
                    self.disconnect()
                } else {
                    self.receiveNext(id)
                }
            }
        }
    }

    /// Processa os bytes recebidos; devolve `false` se a conexão foi encerrada.
    private func handle(_ data: Data) -> Bool {
        let frames: [LinkFrame]
        do {
            frames = try decoder.append(data)
        } catch {
            disconnect()
            return false
        }
        for frame in frames {
            switch frame.messageType {
            case .config:
                if let config = LinkProtocol.parseConfig(frame) {
                    renderer.reset()
                    videoConfig = config
                }
            case .video:
                if let video = LinkProtocol.parseVideo(frame) { renderer.enqueue(video) }
            case .videoEnd:
                // O PC desligou a Segunda Tela (atalho ou bandeja): volta ao relógio, conexão continua.
                if videoConfig != nil {
                    videoConfig = nil
                    renderer.reset()
                }
            default:
                break
            }
            for action in session.receive(frame, now: Date()) {
                switch action {
                case .send(let reply):
                    send(reply)
                case .close:
                    disconnect()
                    return false
                }
            }
        }
        return true
    }

    private func send(_ frame: LinkFrame) {
        connection?.send(content: Data(frame.encoded()), completion: .contentProcessed { _ in })
    }

    private func disconnect() {
        watchdog?.cancel()
        watchdog = nil
        connection?.stateUpdateHandler = nil
        connection?.cancel()
        connection = nil
        isConnected = false
        if videoConfig != nil {
            videoConfig = nil
            renderer.reset()
        }
    }

    /// Resolução nativa em paisagem (largura > altura), em pixels.
    private static func landscapePixels() -> (width: UInt32, height: UInt32) {
        let screen = UIApplication.shared.connectedScenes.compactMap { ($0 as? UIWindowScene)?.screen }.first
        let size = screen?.nativeBounds.size ?? .zero
        return (UInt32(max(size.width, size.height)), UInt32(min(size.width, size.height)))
    }
}
