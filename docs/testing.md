# Testing

## Standalone smoke suite

`tests/UnityMeta.SmokeTests` compiles C# 9 fixtures with Roslyn, runs the actual Cecil
weaver, reloads the transformed assembly and asserts runtime behavior. It also directly
runs the Roslyn source generator and analyzer.

Coverage includes:

- direct field-get transformations that preserve raw storage;
- constant and `nameof` sibling-field clamp transformations;
- ordered set + change composition;
- old/new field-change bindings;
- suppression of notifications when the final value is unchanged;
- named attribute metadata;
- `typeof(...)` metadata;
- before/after method templates;
- return-value observation;
- generated aspect manifest entries;
- analyzer failures covering every current `UMETA001`-`UMETA009` rule.

Run:

```bash
./build.sh
```

Normal GitHub CI repeats the smoke suite on Linux, Windows and macOS.

## Real Unity 2022.3 fixture

`tests/Unity2022Project` is a minimal Unity 2022.3.54f1 project referencing the local UPM
package. Its EditMode tests verify that Unity's actual `ILPostProcessor` path performs:

- field-read transformation;
- field transformation;
- field-change notification and equality filtering;
- after-template return-value observation.

`.github/workflows/unity-2022.yml` is `workflow_dispatch` only because GameCI requires a
Unity license. Configure `UNITY_LICENSE`, `UNITY_EMAIL`, and `UNITY_PASSWORD` repository
secrets before running it.

## Manual game-project sanity checks

Before a stable release, also test a consuming Unity 2022.3 game with:

1. no asmdef (`Assembly-CSharp`);
2. a custom asmdef referencing `UnityMeta.Runtime`;
3. Mono Editor/Player;
4. IL2CPP Player;
5. domain reload enabled and disabled;
6. `UNITYMETA_DISABLE_WEAVING` to confirm the opt-out path;
7. imported Clamp, OnChange and Log samples.

## Packaging verification

```bash
./tools/package-release.sh
```

builds/tests everything, installs the Roslyn companion into the UPM staging package,
creates the UPM `.tgz`, builds `UnityMeta.Authoring.nupkg`, and writes SHA-256 checksums.
