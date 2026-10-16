import SwiftUI
import TelaDoisCore

/// Tela raiz do Modo Descanso.
struct RestScreenView: View {
    let settings: AppSettings

    @State private var now: Date
    @State private var battery: BatteryMonitor
    @State private var weather: WeatherProvider
    /// Diferença entre o relógio exibido e o real (só quando o teste fixa a hora).
    private let clockOffset: TimeInterval
    private let ticker = Timer.publish(every: 1, on: .main, in: .common).autoconnect()

    init(settings: AppSettings) {
        self.settings = settings
        let real = Date()
        let offset = settings.fixedStart.map { $0.timeIntervalSince(real) } ?? 0
        clockOffset = offset
        _now = State(initialValue: real.addingTimeInterval(offset))
        _battery = State(initialValue: BatteryMonitor(fixedLevel: settings.fixedBatteryLevel))
        _weather = State(initialValue: WeatherProvider(fixedTemperature: settings.fixedTemperature))
    }

    var body: some View {
        GeometryReader { geo in
            ZStack {
                Color.black
                ClockPageView(
                    clock: ClockFormatter.text(for: now, timeZone: settings.timeZone),
                    temperature: weather.temperatureLabel,
                    batteryLevel: battery.level,
                    isCharging: battery.isCharging,
                    safeArea: geo.safeAreaInsets
                )
            }
            .ignoresSafeArea()
        }
        .statusBarHidden()
        .persistentSystemOverlays(.hidden)
        .onReceive(ticker) { tick in
            now = tick.addingTimeInterval(clockOffset)
        }
        .onAppear {
            battery.start()
            weather.start()
        }
    }
}
