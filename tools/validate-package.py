#!/usr/bin/env python3
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
package = root / "Packages" / "com.drapnard.unitymeta"

required = [
    package / "package.json",
    package / "Runtime" / "UnityMeta.Runtime.asmdef",
    package / "Editor" / "Unity.DrapNard.UnityMeta.CodeGen.asmdef",
    package / "Editor" / "UnityMetaILPostProcessor.cs",
]

for path in required:
    if not path.exists():
        raise SystemExit(f"missing required package file: {path}")

metadata = json.loads((package / "package.json").read_text(encoding="utf-8"))
if metadata.get("unity") != "2022.3":
    raise SystemExit("Unity 2022.3 must remain the package compatibility baseline")

if metadata.get("name") != "com.drapnard.unitymeta":
    raise SystemExit("unexpected package name")

print("UnityMeta package metadata looks valid.")
