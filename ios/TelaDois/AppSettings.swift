import Foundation

/// Ajustes lidos dos argumentos de lançamento (usados pelos testes de UI), ex.: `-TDDimSeconds 3`.
struct AppSettings {
    var dimSeconds: TimeInterval = 60
    /// O relógio começa nesta data e anda normalmente a partir dela.
    var fixedStart: Date?
    var fixedTemperature: Double?
    /// Nível de bateria fixo entre 0 e 1.
    var fixedBatteryLevel: Double?
    var timeZone: TimeZone = .current

    static func fromLaunchArguments(_ defaults: UserDefaults = .standard) -> AppSettings {
        var settings = AppSettings()
        let seconds = defaults.double(forKey: "TDDimSeconds")
        if seconds > 0 { settings.dimSeconds = seconds }
        if let iso = defaults.string(forKey: "TDFixedDate") {
            settings.fixedStart = ISO8601DateFormatter().date(from: iso)
        }
        if defaults.object(forKey: "TDFixedTemp") != nil {
            settings.fixedTemperature = defaults.double(forKey: "TDFixedTemp")
        }
        if defaults.object(forKey: "TDFixedBattery") != nil {
            settings.fixedBatteryLevel = defaults.double(forKey: "TDFixedBattery")
        }
        if let id = defaults.string(forKey: "TDTimeZone"), let zone = TimeZone(identifier: id) {
            settings.timeZone = zone
        }
        return settings
    }
}
