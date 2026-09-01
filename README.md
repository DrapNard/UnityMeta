# UnityMeta

UnityMeta is an experimental metaprogramming framework for **Unity 2022.3 / C# 9**.
It is designed around the ergonomics of aspect-oriented tools such as Metalama,
but uses Unity-native compilation extension points instead of replacing Unity's
Roslyn compiler.

The package itself is intentionally **not a library of gameplay attributes**.
`Clamp`, `Log`, and similar features live under `Samples~` and demonstrate how a
game or another package can define its own metacode.

## Why this exists

Unity 2022.3 owns its compilation pipeline and uses an older Roslyn toolchain.
Modern Metalama versions expect a modern SDK-style build and compiler integration
that Unity 2022 does not provide. UnityMeta therefore splits metaprogramming into:

1. a tiny runtime authoring API (`FieldSetAspectAttribute`, `MethodAspectAttribute`,
   template/binding attributes),
2. a Unity `ILPostProcessor` backed by Mono.Cecil for behavioral rewriting,
3. an optional Roslyn 3.8 analyzer/source-generator companion for diagnostics and
   future source-visible member introduction.

This keeps ordinary Unity fields as real fields and allows aspects to be authored
inside the same Unity project that consumes them.

## Current MVP

- Custom field-write aspects defined in user code.
- Multiple ordered field-write aspects on the same field.
- Constant aspect constructor arguments injected into templates.
- Dynamic field values resolved from a `nameof(...)` aspect argument without
  runtime reflection.
- Custom method aspects with `Before` and `After` templates.
- Target instance, target argument, member name and type name bindings.
- Unity `ILPostProcessor` integration for assemblies referencing UnityMeta.
- Unity-independent smoke tests for the Cecil weaving core.
- Roslyn 3.8 analyzer/source-generator project.
- UPM package layout for Unity 2022.3.

## Example: a user-defined clamp

```csharp
using UnityMeta;

public sealed class ClampAttribute : FieldSetAspectAttribute
{
    public ClampAttribute(int min, int max) { }
    public ClampAttribute(int min, string maxField) { }

    [SetTemplate]
    public static int Constant(
        [Value] int value,
        [AspectArgument(0)] int min,
        [AspectArgument(1)] int max)
        => value < min ? min : value > max ? max : value;

    [SetTemplate]
    public static int DynamicMax(
        [Value] int value,
        [AspectArgument(0)] int min,
        [FieldValueFromAspectArgument(1)] int max)
        => value < min ? min : value > max ? max : value;
}
```

Usage stays ordinary Unity C#:

```csharp
[Clamp(0, nameof(hpMax))]
public int hp;

public int hpMax = 100;
```

A write such as `hp = 900;` is rewritten after compilation so the selected
`SetTemplate` runs before the `stfld` instruction. `hp` remains a real field.

See [Aspect authoring](docs/aspect-authoring.md) for the full template contract.

## Project status

This first commit is an architectural MVP, not a claim of Metalama feature parity.
The public authoring surface is deliberately small so future backends can add
`Around`/`Proceed`, member introduction, fabrics, compile-time templates, richer
diagnostics and template inlining without forcing gameplay code to depend on
Mono.Cecil.

See [Roadmap](docs/roadmap.md) and [Known limitations](docs/limitations.md).

## Unity installation

For local development, add the package by filesystem path from Unity Package
Manager, or add this to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.drapnard.unitymeta": "file:../../UnityMeta/Packages/com.drapnard.unitymeta"
  }
}
```

Detailed setup: [Unity 2022 setup](docs/unity-2022-setup.md).

## Building tools

The optional compiler companion is built outside Unity:

```bash
./build.sh
```

This builds the .NET projects, runs the Cecil smoke tests, and builds the Roslyn
3.8 compiler companion. Use `./tools/install-compiler.sh` to copy that analyzer
DLL into the UPM package when desired.

## Documentation

- [Architecture](docs/architecture.md)
- [Writing aspects](docs/aspect-authoring.md)
- [Unity 2022 setup](docs/unity-2022-setup.md)
- [PolySharp and language compatibility](docs/polysharp.md)
- [Testing](docs/testing.md)
- [Known limitations](docs/limitations.md)
- [Feature matrix](docs/feature-matrix.md)
- [Architecture decision: Unity-native backend](docs/decisions/0001-unity-native-backend.md)
- [Roadmap](docs/roadmap.md)
- [Contributing](CONTRIBUTING.md)

## License

MIT. See [LICENSE.md](LICENSE.md).
