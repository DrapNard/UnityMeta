# Unity 2022.3 setup

## Baseline

UnityMeta package source is kept compatible with Unity 2022.3 / C# 9. The package depends
on `com.unity.nuget.mono-cecil` 1.11.4. The codegen assembly is Editor-only and has no
UnityEngine reference.

## Preferred: published precompiled UPM tag

```text
https://github.com/DrapNard/UnityMeta.git#upm-v0.2.0-alpha.3
```

This package-root tag includes the Roslyn analyzer/source-generator DLL built by release CI.

UnityMeta also versions `.meta` files for every UPM asset. This is required for Git
dependencies on Unity 2022.3 because their `Library/PackageCache` directory is immutable;
Unity cannot create missing metadata there and otherwise logs `has no meta file, but it's in an immutable folder` before ignoring the asset.

## Local development package

Unity Package Manager -> **Add package from disk...** -> select:

`Packages/com.drapnard.unitymeta/package.json`

Or in a project manifest use a path relative to the project's `Packages` folder:

```json
"com.drapnard.unitymeta": "file:../../UnityMeta/Packages/com.drapnard.unitymeta"
```

For a source checkout, build/install the optional compiler companion:

```bash
./build.sh
./tools/install-compiler.sh
```

The installer creates a `.meta` carrying Unity's `RoslynAnalyzer` label. The Cecil weaving
backend itself does not require the companion.

## Source Git subfolder alternative

Unity Package Manager supports a `path` query for packages stored below a repository root:

```text
https://github.com/DrapNard/UnityMeta.git?path=/Packages/com.drapnard.unitymeta#v0.2.0-alpha.3
```

This source tag does not contain generated binaries committed to Git, so prefer `upm-v*`
for normal consumers.

## Disable weaving temporarily

Add the scripting define:

```text
UNITYMETA_DISABLE_WEAVING
```

The IL post-processor skips assemblies carrying that define.
