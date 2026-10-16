import SwiftUI

@main
struct TelaDoisApp: App {
    private let settings = AppSettings.fromLaunchArguments()

    var body: some Scene {
        WindowGroup {
            RestScreenView(settings: settings)
        }
    }
}
