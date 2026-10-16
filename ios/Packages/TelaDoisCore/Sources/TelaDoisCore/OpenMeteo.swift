import Foundation

/// Temperatura atual pelo Open-Meteo (grátis, sem chave).
public enum OpenMeteo {
    public struct MissingTemperature: Error {}

    private struct Response: Decodable {
        struct Current: Decodable {
            let temperature: Double
            enum CodingKeys: String, CodingKey { case temperature = "temperature_2m" }
        }
        let current: Current?
    }

    public static func currentTemperatureURL(latitude: Double, longitude: Double) -> URL {
        let text = String(
            format: "https://api.open-meteo.com/v1/forecast?latitude=%.3f&longitude=%.3f&current=temperature_2m",
            latitude, longitude
        )
        return URL(string: text)!
    }

    public static func parseCurrentTemperature(_ data: Data) throws -> Double {
        guard let temperature = try JSONDecoder().decode(Response.self, from: data).current?.temperature else {
            throw MissingTemperature()
        }
        return temperature
    }

    public static func label(celsius: Double) -> String {
        "\(Int(celsius.rounded()))°C"
    }
}
