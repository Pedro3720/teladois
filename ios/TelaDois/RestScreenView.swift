import SwiftUI
import TelaDoisCore

/// Tela raiz do Modo Descanso.
struct RestScreenView: View {
    let settings: AppSettings

    @State private var now: Date
    @State private var dim: DimState
    @State private var battery: BatteryMonitor
    @State private var weather: WeatherProvider
    @State private var brightness = ScreenBrightness()
    @State private var link = UsbLink()
    @Environment(\.scenePhase) private var scenePhase
    /// Diferença entre o relógio exibido e o real (só quando o teste fixa a hora).
    private let clockOffset: TimeInterval
    private let ticker = Timer.publish(every: 1, on: .main, in: .common).autoconnect()

    init(settings: AppSettings) {
        self.settings = settings
        let real = Date()
        let offset = settings.fixedStart.map { $0.timeIntervalSince(real) } ?? 0
        clockOffset = offset
        _now = State(initialValue: real.addingTimeInterval(offset))
        _dim = State(initialValue: DimState(now: real, timeout: settings.dimSeconds))
        _battery = State(initialValue: BatteryMonitor(fixedLevel: settings.fixedBatteryLevel))
        _weather = State(initialValue: WeatherProvider(fixedTemperature: settings.fixedTemperature))
    }

    var body: some View {
        GeometryReader { geo in
            if link.isStreaming {
                SecondScreenView(player: link.player)
                    .ignoresSafeArea()
            } else {
                restScreen(safeArea: geo.safeAreaInsets)
            }
        }
        .statusBarHidden()
        .persistentSystemOverlays(.hidden)
        .onReceive(ticker) { tick in
            now = tick.addingTimeInterval(clockOffset)
            // Segunda Tela: o escurecer fica desligado (spec §4.1 item 5).
            if link.isStreaming { dim.touch(now: tick) }
            let wasDimmed = dim.isDimmed
            dim.update(now: tick)
            if dim.isDimmed, !wasDimmed { brightness.dim() }
        }
        .onAppear {
            battery.start()
            weather.start()
            link.start()
            UIApplication.shared.isIdleTimerDisabled = true
        }
        .onChange(of: scenePhase) { _, phase in
            UIApplication.shared.isIdleTimerDisabled = (phase == .active)
            if phase == .active {
                dim.touch(now: Date())
                link.start()
            } else {
                brightness.restore()
            }
            // Só no fundo: puxar a Central de Controle (inactive) não derruba o PC.
            if phase == .background { link.stop() }
        }
        .onChange(of: link.isStreaming) { _, streaming in
            if streaming { brightness.restore() }
        }
    }

    /// Modo Descanso: relógio e, depois do tempo, a camada preta.
    private func restScreen(safeArea: EdgeInsets) -> some View {
        ZStack {
            Color.black
            ClockPageView(
                clock: ClockFormatter.text(for: now, timeZone: settings.timeZone),
                temperature: weather.temperatureLabel,
                batteryLevel: battery.level,
                isCharging: battery.isCharging,
                isLinked: link.isConnected,
                safeArea: safeArea
            )
            if dim.isDimmed {
                // Camada preta por cima de tudo: recebe o toque, então nada embaixo é acionado.
                Color.black
                    .contentShape(Rectangle())
                    .onTapGesture { wake() }
                    // Escurece com fade; acorda na hora (sem fade na saída).
                    .transition(.asymmetric(
                        insertion: .opacity.animation(.easeIn(duration: 1.2)),
                        removal: .identity
                    ))
                    .accessibilityElement()
                    .accessibilityIdentifier("dim-overlay")
                    .accessibilityLabel("Tela escurecida")
                    .accessibilityHint("Toque para acender")
                    .accessibilityAddTraits(.isButton)
            }
        }
        .ignoresSafeArea()
        // Qualquer toque com a tela acesa reinicia a contagem de 60 s.
        .simultaneousGesture(
            DragGesture(minimumDistance: 0).onChanged { _ in
                if !dim.isDimmed { dim.touch(now: Date()) }
            }
        )
    }

    private func wake() {
        dim.touch(now: Date())
        brightness.restore()
    }
}
