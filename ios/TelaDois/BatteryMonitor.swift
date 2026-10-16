import UIKit
import Observation

/// Nível e estado de carga da bateria (fixos quando o teste pede).
@MainActor
@Observable
final class BatteryMonitor {
    private(set) var level: Double = 1
    private(set) var isCharging = false

    @ObservationIgnored private let fixedLevel: Double?
    @ObservationIgnored private var observers: [NSObjectProtocol] = []

    init(fixedLevel: Double?) {
        self.fixedLevel = fixedLevel
    }

    func start() {
        if let fixedLevel {
            level = fixedLevel
            return
        }
        guard observers.isEmpty else { return }
        UIDevice.current.isBatteryMonitoringEnabled = true
        refresh()
        for name in [UIDevice.batteryLevelDidChangeNotification, UIDevice.batteryStateDidChangeNotification] {
            let token = NotificationCenter.default.addObserver(forName: name, object: nil, queue: .main) { [weak self] _ in
                MainActor.assumeIsolated { self?.refresh() }
            }
            observers.append(token)
        }
    }

    private func refresh() {
        let device = UIDevice.current
        // O simulador informa -1 (desconhecido): mostra cheio.
        level = device.batteryLevel < 0 ? 1 : Double(device.batteryLevel)
        isCharging = device.batteryState == .charging || device.batteryState == .full
    }
}
