import SwiftUI

struct BatteryRingView: View {
    /// Entre 0 e 1.
    let level: Double
    let isCharging: Bool

    private var percent: Int { Int((min(max(level, 0), 1) * 100).rounded()) }

    var body: some View {
        GeometryReader { geo in
            let side = min(geo.size.width, geo.size.height)
            let line = side * 4.5 / 44
            ZStack {
                Circle().stroke(Theme.track, lineWidth: line)
                Circle()
                    .trim(from: 0, to: CGFloat(percent) / 100)
                    .stroke(Theme.battery, style: StrokeStyle(lineWidth: line, lineCap: .round))
                    .rotationEffect(.degrees(-90))
                Group {
                    if isCharging {
                        Image(systemName: "bolt.fill")
                    } else {
                        Text("\(percent)")
                    }
                }
                .font(.system(size: side * 0.3, weight: .bold, design: .rounded))
                .foregroundStyle(Theme.battery)
            }
            .padding(line / 2)
        }
        .accessibilityElement(children: .ignore)
        .accessibilityIdentifier("battery")
        .accessibilityLabel("Bateria")
        .accessibilityValue("\(percent)%")
    }
}
