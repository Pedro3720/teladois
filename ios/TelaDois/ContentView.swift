import SwiftUI

struct ContentView: View {
    var body: some View {
        ZStack {
            Color.black
            Text(Greeting.text)
                .font(.system(size: 64, weight: .bold, design: .rounded))
                .foregroundStyle(.white)
                .accessibilityIdentifier("greeting")
        }
        .ignoresSafeArea()
        .statusBarHidden()
        .persistentSystemOverlays(.hidden)
    }
}
