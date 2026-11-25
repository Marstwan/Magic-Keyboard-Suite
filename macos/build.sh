#!/usr/bin/env bash
# Compila, firma y empaqueta Magic Keyboard Suite para macOS (.app + .dmg).
# Uso: ./macos/build.sh [--dmg]

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MAC_DIR="$ROOT_DIR/macos"
VERSION="$(tr -d '[:space:]' < "$ROOT_DIR/VERSION")"

echo "==> Generando tokens, disposiciones y textos compartidos (v$VERSION)..."
python3 "$ROOT_DIR/shared/tools/generate.py"

APP_BUNDLE="$MAC_DIR/Magic Keyboard Suite.app"
CONTENTS="$APP_BUNDLE/Contents"
MACOS_BIN="$CONTENTS/MacOS"
RESOURCES="$CONTENTS/Resources"
SWIFT_CACHE="$MAC_DIR/.build/swift-cache"

mkdir -p "$MACOS_BIN" "$RESOURCES" "$SWIFT_CACHE"

echo "==> Compilando MagicKeyboardSuite (SwiftUI nativo)..."
SWIFT_FILES=()
while IFS= read -r -d '' f; do
    SWIFT_FILES+=("$f")
done < <(find "$MAC_DIR/Sources/MagicKeyboardSuite" -name '*.swift' -print0 | sort -z)

swiftc \
    -O \
    -module-cache-path "$SWIFT_CACHE" \
    -sdk "$(xcrun --show-sdk-path)" \
    -target "$(uname -m)-apple-macos13.0" \
    -framework SwiftUI \
    -framework AppKit \
    -framework IOBluetooth \
    -framework IOKit \
    -framework Carbon \
    -framework ServiceManagement \
    "${SWIFT_FILES[@]}" \
    -o "$MACOS_BIN/MagicKeyboardSuite"

cp "$ROOT_DIR/shared/assets/AppIcon.icns" "$RESOURCES/AppIcon.icns"

cat > "$CONTENTS/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleDevelopmentRegion</key>
    <string>en</string>
    <key>CFBundleDisplayName</key>
    <string>Magic Keyboard Suite</string>
    <key>CFBundleExecutable</key>
    <string>MagicKeyboardSuite</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon</string>
    <key>CFBundleIdentifier</key>
    <string>com.magickeyboardsuite.mac</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundleName</key>
    <string>Magic Keyboard Suite</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>$VERSION</string>
    <key>CFBundleVersion</key>
    <string>$VERSION</string>
    <key>LSMinimumSystemVersion</key>
    <string>13.0</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSBluetoothAlwaysUsageDescription</key>
    <string>Magic Keyboard Suite necesita acceso a Bluetooth para mostrar tus teclados y permitirte desconectarlos, liberarlos para Windows o reconectarlos en este Mac.</string>
</dict>
</plist>
EOF

echo "==> Firmando aplicación..."
SIGN_IDENTITY="${CODESIGN_IDENTITY:--}"
codesign --force --deep --sign "$SIGN_IDENTITY" "$APP_BUNDLE"

if [[ "${1:-}" == "--dmg" ]]; then
    DMG_PATH="$MAC_DIR/MagicKeyboardSuite-macOS.dmg"
    rm -f "$DMG_PATH"
    echo "==> Creando imagen de disco $DMG_PATH..."
    hdiutil create -volname "Magic Keyboard Suite" -srcfolder "$APP_BUNDLE" -ov -format UDZO "$DMG_PATH"
fi

echo "==> Listo: $APP_BUNDLE (v$VERSION)"
