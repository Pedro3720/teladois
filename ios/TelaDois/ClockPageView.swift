import SwiftUI
import TelaDoisCore

/// Página 1 do Modo Descanso: relógio gigante à esquerda; data, temperatura e bateria à direita.
struct ClockPageView: View {
    let clock: ClockText
    let temperature: String?
    let batteryLevel: Double
    let isCharging: Bool
    /// PC conectado pelo cabo (Etapa 4: só o aviso; a Segunda Tela vem na Etapa 5).
    let isLinked: Bool
    /// Área segura da tela (a página em si ignora a área segura e aplica as folgas manualmente).
    let safeArea: EdgeInsets

    var body: some View {
        GeometryReader { geo in
            let height = geo.size.height
            let width = geo.size.width
            HStack(alignment: .center, spacing: width * 0.04) {
                Text("\(clock.hours):\(clock.minutes)")
                    .font(.system(size: height * 0.95, weight: .bold).width(.compressed))
                    .monospacedDigit()
                    .foregroundStyle(Theme.clock)
                    .lineLimit(1)
                    .minimumScaleFactor(0.3)
                    // O protótipo estica o relógio na vertical (1,5×); sem isso a fonte, limitada pela largura, fica baixa.
                    .scaleEffect(x: 1, y: 1.5)
                    // Identificador no próprio Text (antes do .frame) para o teste medir o texto, não a coluna.
                    .accessibilityIdentifier("clock")
                    .frame(maxWidth: .infinity, maxHeight: .infinity)

                VStack(alignment: .leading, spacing: 0) {
                    HStack(alignment: .top, spacing: 12) {
                        VStack(alignment: .leading, spacing: 0) {
                            (Text(clock.weekday).foregroundStyle(Theme.clock)
                                + Text(", \(clock.day)").foregroundStyle(Theme.ink))
                                .accessibilityIdentifier("date")
                            if let temperature {
                                Text(temperature)
                                    .foregroundStyle(Theme.ink)
                                    .accessibilityIdentifier("temp")
                            }
                        }
                        .font(.system(size: height * 0.095, weight: .semibold, design: .rounded))
                        .lineLimit(1)
                        .minimumScaleFactor(0.7)
                        Spacer(minLength: 0)
                        BatteryRingView(level: batteryLevel, isCharging: isCharging)
                            .frame(width: height * 0.22, height: height * 0.22)
                    }
                    Spacer(minLength: 0)
                    if isLinked {
                        Text("PC conectado")
                            .font(.system(size: height * 0.06, weight: .semibold, design: .rounded))
                            .foregroundStyle(Theme.muted)
                            .lineLimit(1)
                            .accessibilityIdentifier("link-status")
                    }
                }
                .frame(width: width * 0.34)
            }
            .padding(.leading, max(safeArea.leading + 12, Theme.sideMargin))
            .padding(.trailing, max(safeArea.trailing + 12, Theme.rightClearance))
            .padding(.top, max(safeArea.top + 12, Theme.topClearance))
            .padding(.bottom, max(safeArea.bottom + 12, Theme.bottomMargin))
        }
    }
}
