import Darwin
import Foundation
import TelaDoisCore

/// Faz o papel do PC nos testes de UI: conecta em 127.0.0.1:7100, onde o app escuta.
final class FakePC {
    private let socketFD: Int32
    private var decoder = LinkFrameDecoder()
    private var pending: [LinkFrame] = []

    private init(socketFD: Int32) {
        self.socketFD = socketFD
    }

    deinit {
        close(socketFD)
    }

    /// Tenta conectar por até `timeout` segundos (o listener sobe logo depois que o app abre).
    static func connect(port: UInt16 = LinkProtocol.port, timeout: TimeInterval = 5) -> FakePC? {
        let deadline = Date().addingTimeInterval(timeout)
        repeat {
            let fd = socket(AF_INET, SOCK_STREAM, 0)
            if fd >= 0 {
                var address = sockaddr_in()
                address.sin_family = sa_family_t(AF_INET)
                address.sin_port = port.bigEndian
                address.sin_addr.s_addr = inet_addr("127.0.0.1")
                let result = withUnsafePointer(to: &address) {
                    $0.withMemoryRebound(to: sockaddr.self, capacity: 1) {
                        Darwin.connect(fd, $0, socklen_t(MemoryLayout<sockaddr_in>.size))
                    }
                }
                if result == 0 {
                    var receiveTimeout = timeval(tv_sec: 5, tv_usec: 0)
                    _ = setsockopt(fd, SOL_SOCKET, SO_RCVTIMEO, &receiveTimeout, socklen_t(MemoryLayout<timeval>.size))
                    return FakePC(socketFD: fd)
                }
                close(fd)
            }
            Thread.sleep(forTimeInterval: 0.2)
        } while Date() < deadline
        return nil
    }

    func send(_ frame: LinkFrame) {
        let bytes = frame.encoded()
        _ = bytes.withUnsafeBytes { Darwin.send(socketFD, $0.baseAddress, $0.count, 0) }
    }

    /// Próxima mensagem do app; `nil` se a conexão fechou ou ficou 5 s sem dados.
    func readFrame() -> LinkFrame? {
        while pending.isEmpty {
            var buffer = [UInt8](repeating: 0, count: 4096)
            let count = recv(socketFD, &buffer, buffer.count, 0)
            guard count > 0 else { return nil }
            guard let frames = try? decoder.append(buffer[0..<count]) else { return nil }
            pending += frames
        }
        return pending.removeFirst()
    }
}
