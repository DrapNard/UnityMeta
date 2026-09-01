#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

git diff --check
python3 tools/validate-package.py

if command -v dotnet >/dev/null 2>&1; then
  ./build.sh
else
  echo "dotnet not found; skipped executable .NET verification." >&2
fi

echo "Repository verification completed."
