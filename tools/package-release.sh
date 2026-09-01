#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

VERSION="${1:-}"
if [[ -z "$VERSION" ]]; then
  VERSION="$(python3 - <<'PY'
import json
from pathlib import Path
print(json.loads(Path('Packages/com.drapnard.unitymeta/package.json').read_text())['version'])
PY
)"
fi

PACKAGE_VERSION="$(python3 - <<'PY'
import json
from pathlib import Path
print(json.loads(Path('Packages/com.drapnard.unitymeta/package.json').read_text())['version'])
PY
)"

if [[ "$VERSION" != "$PACKAGE_VERSION" ]]; then
  echo "Requested release version '$VERSION' does not match UPM package version '$PACKAGE_VERSION'." >&2
  exit 1
fi

./build.sh
./tools/install-compiler.sh
python3 tools/validate-package.py

ARTIFACTS="$ROOT/artifacts"
STAGING="$ROOT/.release-staging"
rm -rf "$ARTIFACTS" "$STAGING"
mkdir -p "$ARTIFACTS" "$STAGING/package"

cp -a Packages/com.drapnard.unitymeta/. "$STAGING/package/"
tar -C "$STAGING" -czf "$ARTIFACTS/com.drapnard.unitymeta-$VERSION.tgz" package

# The NuGet package intentionally contains only authoring/runtime + Roslyn
# diagnostics. IL weaving remains a Unity UPM responsibility.
dotnet pack pack/UnityMeta.Authoring/UnityMeta.Authoring.csproj \
  -c Release \
  -p:PackageVersion="$VERSION" \
  -o "$ARTIFACTS"

(
  cd "$ARTIFACTS"
  sha256sum ./* > SHA256SUMS.txt
)

rm -rf "$STAGING"
echo "Release artifacts created in $ARTIFACTS"
