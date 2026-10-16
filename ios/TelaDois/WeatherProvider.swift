import CoreLocation
import Observation
import TelaDoisCore

/// Temperatura atual: localização (quando permitida) + Open-Meteo a cada 30 min.
/// Sem permissão, sem rede ou com resposta inválida, `temperatureLabel` fica `nil` (a tela esconde).
@MainActor
@Observable
final class WeatherProvider {
    private(set) var temperatureLabel: String?

    @ObservationIgnored private let fixedTemperature: Double?
    @ObservationIgnored private let manager = CLLocationManager()
    @ObservationIgnored private var task: Task<Void, Never>?

    static let refreshInterval: Duration = .seconds(30 * 60)

    init(fixedTemperature: Double?) {
        self.fixedTemperature = fixedTemperature
    }

    func start() {
        if let fixedTemperature {
            temperatureLabel = OpenMeteo.label(celsius: fixedTemperature)
            return
        }
        guard task == nil else { return }
        manager.requestWhenInUseAuthorization()
        task = Task { [weak self] in
            while !Task.isCancelled {
                await self?.refresh()
                try? await Task.sleep(for: WeatherProvider.refreshInterval)
            }
        }
    }

    private func refresh() async {
        do {
            for try await update in CLLocationUpdate.liveUpdates() {
                guard let location = update.location else { continue }
                let url = OpenMeteo.currentTemperatureURL(
                    latitude: location.coordinate.latitude,
                    longitude: location.coordinate.longitude
                )
                let (data, _) = try await URLSession.shared.data(from: url)
                temperatureLabel = OpenMeteo.label(celsius: try OpenMeteo.parseCurrentTemperature(data))
                return
            }
        } catch {
            // Mantém o último valor (ou nenhum); tenta de novo no próximo ciclo.
        }
    }
}
