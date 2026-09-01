# Roadmap

## 0.2 - Template inlining and diagnostics

- Inline eligible set/boundary template IL instead of emitting runtime helper calls.
- Rich weave diagnostics with source/PDB locations.
- Validate ambiguous/no-template matches in Roslyn before Unity reaches ILPP.
- More aspect argument constant kinds (`Type`, arrays where legal).
- Golden IL snapshot tests.

## 0.3 - Around aspects

- Template IR independent of Cecil instructions.
- `AroundMethodAspect`.
- `meta.Proceed()`.
- Return-value binding and rewriting.
- Exception-safe `After`/`Finally` templates.
- Constructor support.

## 0.4 - Source-visible introduction

- Roslyn 3.8 source backend for generated members visible to the same compilation.
- Introduce methods/properties/interfaces.
- Stable generated symbol naming.
- Cross-link generated symbols with the IL backend.

## 0.5 - Composition and project-wide aspects

- Aspect dependencies and conflict diagnostics.
- Explicit compile-time/run-time ordering model.
- Type and assembly aspects.
- Fabric-like selectors to apply aspects by namespace/type/member predicate.
- Transitive dependency/revalidation graph primitives.

## 0.6 - Unity-specific authoring

- Inspector validation hooks for field constraints.
- Property drawers generated/registered from aspect metadata.
- SerializedProperty-aware validation.
- Better support for structs and serialized nested objects.

## Long-term

Aim for the useful authoring power of Metalama while keeping Unity's compiler
untouched. Replacing/forking Unity Roslyn remains a non-goal unless Unity exposes
a stable compiler substitution contract.
