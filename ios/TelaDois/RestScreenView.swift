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
            ZStack {
                Color.black
                if link.videoConfig != nil {
                    SecondScreenView(renderer: link.renderer)
                        .accessibilityElement()
                        .accessibilityIdentifier("second-screen")
                        .accessibilityLabel("Segunda Tela")
                } else {
                    ClockPageView(
                        clock: ClockFormatter.text(for: now, timeZone: settings.timeZone),
                        temperature: weather.temperatureLabel,
                        batteryLevel: battery.level,
                        isCharging: battery.isCharging,
                        isLinked: link.isConnected,
                        safeArea: geo.safeAreaInsets
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
            }
            .ignoresSafeArea()
            // Qualquer toque com a tela acesa reinicia a contagem de 60 s.
            .simultaneousGesture(
                DragGesture(minimumDistance: 0).onChanged { _ in
                    if !dim.isDimmed { dim.touch(now: Date()) }
                }
            )
        }
        .statusBarHidden()
        .persistentSystemOverlays(.hidden)
        .onReceive(ticker) { tick in
            now = tick.addingTimeInterval(clockOffset)
            if link.videoConfig != nil {
                // Segunda Tela: nunca escurece e usa o brilho do usuário.
                dim.touch(now: tick)
                brightness.restore()
                return
            }
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
    }

    private func wake() {
        dim.touch(now: Date())
        brightness.restore()
    }
}
