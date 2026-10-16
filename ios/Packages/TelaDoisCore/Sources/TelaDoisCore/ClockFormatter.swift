import Foundation

public struct ClockText: Equatable, Sendable {
    public let hours: String
    public let minutes: String
    public let weekday: String
    public let day: String

    public init(hours: String, minutes: String, weekday: String, day: String) {
        self.hours = hours
        self.minutes = minutes
        self.weekday = weekday
        self.day = day
    }
}

/// Textos do relógio do Modo Descanso: "10", "51", "QUA.", "14" (24 h, pt-BR).
public enum ClockFormatter {
    public static func text(for date: Date, timeZone: TimeZone, locale: Locale = Locale(identifier: "pt_BR")) -> ClockText {
        var calendar = Calendar(identifier: .gregorian)
        calendar.timeZone = timeZone
        let parts = calendar.dateComponents([.hour, .minute, .day], from: date)

        let weekdayFormatter = DateFormatter()
        weekdayFormatter.locale = locale
        weekdayFormatter.timeZone = timeZone
        weekdayFormatter.dateFormat = "EEE"
        var weekday = weekdayFormatter.string(from: date).uppercased(with: locale)
        if !weekday.hasSuffix(".") { weekday += "." }

        return ClockText(
            hours: String(format: "%02d", parts.hour ?? 0),
            minutes: String(format: "%02d", parts.minute ?? 0),
            weekday: weekday,
            day: String(parts.day ?? 0)
        )
    }
}
