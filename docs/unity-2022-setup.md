# Unity 2022.3 setup

## Baseline

UnityMeta targets Unity 2022.3 and C# 9 syntax in package source.

The UPM package depends on `com.unity.nuget.mono-cecil` 1.11.4, a version shipped
in the Unity 2022 generation. The codegen asmdef is Editor-only and deliberately
has no UnityEngine reference.

## Install as a local package

Unity Package Manager -> **Add package from disk...** -> select:

`Packages/com.drapnard.unitymeta/package.json`

Or reference a checkout from your project's `Packages/manifest.json`:

```json
"com.drapnard.unitymeta": "file:../../UnityMeta/Packages/com.drapnard.unitymeta"
```

## Optional Roslyn companion

Unity 2022's supported analyzer/source-generator workflow requires a .NET
Standard 2.0 analyzer using Microsoft.CodeAnalysis 3.8.

Build and install it:

```bash
./build.sh
./tools/install-compiler.sh
```

The install script creates a Unity `.meta` with the `RoslynAnalyzer` label.
The IL weaving core does not require this companion in v0.1.

## Disable weaving temporarily

Add the scripting define:

```text
UNITYMETA_DISABLE_WEAVING
```

The IL post-processor will skip the assembly.

## Why the codegen assembly is named `Unity.*.CodeGen`

Unity's IL post-processing assembly resolution has historically expected codegen
assemblies to follow this naming pattern for compilation-pipeline references.
The package uses `Unity.DrapNard.UnityMeta.CodeGen` accordingly.
