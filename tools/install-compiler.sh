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
  # Roslyn analyzers are compiler inputs, not runtime/editor plugins. Unity's
  # PluginImporter must not try to load or validate them as normal managed plugins.
  validateReferences: 0
  platformData:
  - first:
      : Any
    second:
      enabled: 0
      settings:
        Exclude Editor: 1
        Exclude Linux64: 1
        Exclude OSXUniversal: 1
        Exclude Win: 1
        Exclude Win64: 1
  - first:
      Any:
    second:
      enabled: 0
      settings: {}
  - first:
      Editor: Editor
    second:
      enabled: 0
      settings:
        CPU: AnyCPU
        DefaultValueInitialized: true
        OS: AnyOS
  - first:
      Standalone: Linux64
    second:
      enabled: 0
      settings:
        CPU: None
  - first:
      Standalone: OSXUniversal
    second:
      enabled: 0
      settings:
        CPU: None
  - first:
      Standalone: Win
    second:
      enabled: 0
      settings:
        CPU: None
  - first:
      Standalone: Win64
    second:
      enabled: 0
      settings:
        CPU: None
  userData:
  assetBundleName:
  assetBundleVariant:
EOF

echo "Installed UnityMeta.Compiler.dll as a RoslynAnalyzer asset."
