#!/usr/bin/env python3
import json
import re
from pathlib import Path

root = Path(__file__).resolve().parents[1]
package = root / "Packages" / "com.drapnard.unitymeta"

required = [
    package / "package.json",
    package / "Runtime" / "UnityMeta.Runtime.asmdef",
    package / "Editor" / "Unity.DrapNard.UnityMeta.CodeGen.asmdef",
    package / "Editor" / "UnityMetaILPostProcessor.cs",
    package / "Documentation~" / "index.md",
    package / "README.md",
    package / "LICENSE.md",
]

for path in required:
    if not path.exists():
        raise SystemExit(f"missing required package file: {path}")

metadata = json.loads((package / "package.json").read_text(encoding="utf-8"))
if metadata.get("unity") != "2022.3":
    raise SystemExit("Unity 2022.3 must remain the package compatibility baseline")

if metadata.get("name") != "com.drapnard.unitymeta":
    raise SystemExit("unexpected package name")

version = metadata.get("version", "")
if not re.fullmatch(r"\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?", version):
    raise SystemExit(f"package version is not valid SemVer: {version!r}")

if metadata.get("dependencies", {}).get("com.unity.nuget.mono-cecil") != "1.11.4":
    raise SystemExit("unexpected Mono.Cecil UPM dependency")

for sample in metadata.get("samples", []):
    sample_path = package / sample.get("path", "")
    if not sample_path.is_dir():
        raise SystemExit(f"sample path does not exist: {sample_path}")

runtime_asmdef = json.loads((package / "Runtime" / "UnityMeta.Runtime.asmdef").read_text(encoding="utf-8"))
if runtime_asmdef.get("name") != "UnityMeta.Runtime":
    raise SystemExit("runtime asmdef name must remain UnityMeta.Runtime")

codegen_asmdef = json.loads(
    (package / "Editor" / "Unity.DrapNard.UnityMeta.CodeGen.asmdef").read_text(encoding="utf-8")
)
codegen_name = codegen_asmdef.get("name", "")
if not (codegen_name.startswith("Unity.") and codegen_name.endswith(".CodeGen")):
    raise SystemExit("codegen assembly must follow the Unity.*.CodeGen naming pattern")
if codegen_asmdef.get("includePlatforms") != ["Editor"]:
    raise SystemExit("codegen assembly must remain Editor-only")
if not codegen_asmdef.get("noEngineReferences"):
    raise SystemExit("codegen assembly must not depend on UnityEngine")

analyzer = package / "Editor" / "Analyzers" / "UnityMeta.Compiler.dll"
if analyzer.exists():
    meta = analyzer.with_suffix(analyzer.suffix + ".meta")
    if not meta.exists() or "RoslynAnalyzer" not in meta.read_text(encoding="utf-8"):
        raise SystemExit("installed compiler DLL must have a RoslynAnalyzer .meta label")

print(f"UnityMeta package metadata looks valid ({version}).")
