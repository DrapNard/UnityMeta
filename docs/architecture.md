# Architecture

## Goal

UnityMeta provides a reusable **metacode authoring system**, not a fixed catalog
of gameplay attributes. A consumer defines an attribute and one or more templates;
UnityMeta discovers the aspect in the compiled assembly and changes the target IL.

`Clamp` and `Log` are samples only.

## Why not fork Metalama directly?

Modern Metalama integrates deeply with a modern Roslyn compiler and SDK-style
build. Unity 2022.3 owns an older compiler pipeline. Replacing Unity's compiler
would couple the project to editor internals, asmdef behavior, Burst/IL2CPP,
incremental compilation and each Unity patch release.

UnityMeta therefore borrows the *aspect authoring model*, not the compiler backend.
The backend uses extension points Unity already exposes.

## Layers

### Runtime authoring API

`Packages/com.drapnard.unitymeta/Runtime`

Contains only attributes and metadata contracts. It has no UnityEngine dependency
and no Cecil dependency. User-authored aspects derive from field-get, field-set,
field-change or method aspect bases.

### Weaver core

`Editor/Weaver/Core`

Pure Mono.Cecil transformation engine. It can run inside Unity or in ordinary
.NET tests. It discovers aspect attributes by inheritance and composes templates.

### Unity IL post-processor

`Editor/UnityMetaILPostProcessor.cs`

Adapts `ICompiledAssembly` to Cecil, runs the core, writes the transformed PE/PDB,
and reports Unity compilation diagnostics. The codegen assembly name intentionally
starts with `Unity.` and ends with `.CodeGen`, matching the established Unity
IL-post-processing pattern.

### Roslyn compiler companion

`compiler/UnityMeta.Compiler`

Targets .NET Standard 2.0 and Microsoft.CodeAnalysis 3.8. It currently supplies:

- early template diagnostics;
- an aspect manifest source generator.

This layer is optional for the current IL weaving path. It is the planned backend for features
that must be visible to C# **before** IL exists, such as introduced members and
interfaces.

## Field-get transformation

A `FieldGetAspectAttribute` with a compatible `[GetTemplate]` rewrites ordinary
`ldfld`/`ldsfld` loads. The raw loaded value is captured in a local, ordered get templates
transform that local, and the final transformed value is pushed back onto the evaluation
stack. Storage itself is never mutated by a get aspect.

Instance loads preserve the original instruction as the instance-capture anchor so existing
branch/exception targets do not jump past injected code. Prefix-sensitive and address-taking
field operations are deliberately left untouched in the current alpha.

## Field-set transformation

Given:

```csharp
[MyAspect(...)]
public int value;
```

and a compatible `[SetTemplate]`, every `stfld`/`stsfld` processed by UnityMeta is
rewritten conceptually from:

```text
instance, value -> stfld
```

to:

```text
store instance/value in locals
value = Template(value, bound metadata...)
instance, value -> stfld
```

The original store instruction is reused as the first local store so branch
targets that pointed at it do not skip the injected transformation.

Multiple set aspects are ordered by `MetaAspectAttribute.Order` and chained.

## Field-change transformation

`FieldChangeAspectAttribute` is processed in the same field-store rewrite. The weaver
captures the old field value before transformations, stores the final transformed value,
then compares old/final values with `EqualityComparer<T>.Default`. Compatible
`[ChangeTemplate]` calls execute only when the values differ. This makes OnChange-style
metacode a framework primitive rather than a hard-coded gameplay attribute.

## Dynamic metadata without reflection

`FieldValueFromAspectArgument` treats an aspect constructor argument as a sibling
field name. The weaver resolves the string at weave time and emits `ldfld` or
`ldsfld` directly. Runtime code performs no `FieldInfo` lookup.

This is why a sample can express:

```csharp
[Clamp(0, nameof(hpMax))]
public int hp;
```

while the clamp implementation receives the current `hpMax` value.

## Method boundary transformation

A method aspect can provide static `[BeforeTemplate]` and `[AfterTemplate]`
methods. The weaver injects before calls at method entry and after calls before
each `ret`. After templates are composed in reverse aspect order. If an after template
uses `[ReturnValue]`, the weaver temporarily stores the return stack value in a local,
passes it to templates, then reloads the unchanged value for `ret`.

This MVP intentionally stops short of `Around`/`meta.Proceed()`. Correctly moving
arbitrary method bodies, exception regions, async/iterator state machines and
return values requires a dedicated template IR/inliner and is tracked in the
roadmap.

## Future source/IL split

Features fall into two categories:

- **Behavioral changes** to code that already exists -> IL backend.
- **New C# symbols** that consuming source must resolve -> Roslyn source backend.

The public aspect API should hide that distinction as much as possible.
