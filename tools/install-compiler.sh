#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SOURCE="$ROOT/compiler/UnityMeta.Compiler/bin/Release/netstandard2.0/UnityMeta.Compiler.dll"
DEST_DIR="$ROOT/Packages/com.drapnard.unitymeta/Editor/Analyzers"
DEST="$DEST_DIR/UnityMeta.Compiler.dll"
META="$DEST.meta"

if [[ ! -f "$SOURCE" ]]; then
  echo "Compiler DLL not found. Run ./build.sh first." >&2
  exit 1
fi

mkdir -p "$DEST_DIR"
cp "$SOURCE" "$DEST"

cat > "$META" <<'EOF'
fileFormatVersion: 2
guid: 6ba30249297d49bb851746aa61d73240
labels:
- RoslynAnalyzer
PluginImporter:
  externalObjects: {}
  serializedVersion: 2
  iconMap: {}
  executionOrder: {}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 0
  isExplicitlyReferenced: 0
  validateReferences: 1
  platformData:
  - first:
      Any:
    second:
      enabled: 1
      settings: {}
  userData:
  assetBundleName:
  assetBundleVariant:
EOF

echo "Installed UnityMeta.Compiler.dll as a RoslynAnalyzer asset."
