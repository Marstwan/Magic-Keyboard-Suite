// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "MagicKeyboardSuite",
    defaultLocalization: "es",
    platforms: [
        .macOS(.v13)
    ],
    products: [
        .executable(name: "MagicKeyboardSuite", targets: ["MagicKeyboardSuite"])
    ],
    targets: [
        .executableTarget(
            name: "MagicKeyboardSuite",
            path: "Sources/MagicKeyboardSuite",
            linkerSettings: [
                .linkedFramework("SwiftUI"),
                .linkedFramework("AppKit"),
                .linkedFramework("IOBluetooth"),
                .linkedFramework("IOKit"),
                .linkedFramework("Carbon"),
                .linkedFramework("ServiceManagement")
            ]
        )
    ]
)
