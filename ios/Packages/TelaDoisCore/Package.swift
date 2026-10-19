// swift-tools-version: 6.0
import PackageDescription

let package = Package(
    name: "TelaDoisCore",
    platforms: [.iOS(.v17), .macOS(.v14)],
    products: [.library(name: "TelaDoisCore", targets: ["TelaDoisCore"])],
    targets: [
        .target(name: "TelaDoisCore"),
        .testTarget(name: "TelaDoisCoreTests", dependencies: ["TelaDoisCore"], resources: [.copy("Fixtures")]),
    ]
)
