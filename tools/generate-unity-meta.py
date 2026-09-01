#!/usr/bin/env python3
"""Generate stable Unity .meta files for the UPM package.

Git-backed UPM dependencies are immutable inside Library/PackageCache. Unity 2022.3
will not create missing .meta files there and ignores those assets instead, so every
asset shipped by UnityMeta must already have stable metadata in the repository.

The generated GUID only seeds metadata for files that do not have a .meta yet. Once
created, move the .meta together with its asset on rename so the GUID stays stable.
"""

from __future__ import annotations

import hashlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PACKAGE = ROOT / "Packages" / "com.drapnard.unitymeta"


def stable_guid(relative_asset_path: str) -> str:
    seed = f"com.drapnard.unitymeta:{relative_asset_path}".encode("utf-8")
    return hashlib.sha256(seed).hexdigest()[:32]


def folder_meta(guid: str) -> str:
    return f"""fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def mono_meta(guid: str) -> str:
    return f"""fileFormatVersion: 2
guid: {guid}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def asmdef_meta(guid: str) -> str:
    return f"""fileFormatVersion: 2
guid: {guid}
AssemblyDefinitionImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def package_manifest_meta(guid: str) -> str:
    return f"""fileFormatVersion: 2
guid: {guid}
PackageManifestImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def text_meta(guid: str) -> str:
    return f"""fileFormatVersion: 2
guid: {guid}
TextScriptImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def default_meta(guid: str) -> str:
    return f"""fileFormatVersion: 2
guid: {guid}
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def meta_for_file(path: Path, guid: str) -> str:
    if path.name == "package.json":
        return package_manifest_meta(guid)
    if path.suffix == ".cs":
        return mono_meta(guid)
    if path.suffix == ".asmdef":
        return asmdef_meta(guid)
    if path.suffix.lower() in {".md", ".txt", ".json"}:
        return text_meta(guid)
    return default_meta(guid)


def main() -> None:
    if not PACKAGE.is_dir():
        raise SystemExit(f"package directory not found: {PACKAGE}")

    created: list[Path] = []

    # Parent folders are assets too. Git/UPM packages are immutable, so Unity cannot
    # synthesize their metadata after download.
    directories = sorted(
        (p for p in PACKAGE.rglob("*") if p.is_dir()),
        key=lambda p: (len(p.parts), p.as_posix()),
    )
    for directory in directories:
        meta = directory.with_name(directory.name + ".meta")
        if meta.exists():
            continue
        relative = directory.relative_to(PACKAGE).as_posix()
        meta.write_text(folder_meta(stable_guid(relative)), encoding="utf-8")
        created.append(meta)

    files = sorted(
        p for p in PACKAGE.rglob("*") if p.is_file() and not p.name.endswith(".meta")
    )
    for asset in files:
        meta = asset.with_name(asset.name + ".meta")
        if meta.exists():
            continue
        relative = asset.relative_to(PACKAGE).as_posix()
        meta.write_text(meta_for_file(asset, stable_guid(relative)), encoding="utf-8")
        created.append(meta)

    if created:
        print(f"Generated {len(created)} Unity .meta files:")
        for path in created:
            print(f"  {path.relative_to(ROOT)}")
    else:
        print("All UnityMeta package assets already have .meta files.")


if __name__ == "__main__":
    main()
