# ADR 0001: Use a Unity-native backend instead of forking Metalama.Compiler

- Status: Accepted
- Date: 2026-09-01

## Context

The project wants Metalama-like aspect authoring on Unity 2022.3. Modern Metalama
is coupled to a newer Roslyn/compiler pipeline and SDK-style build assumptions.
Unity owns its script compilation pipeline and exposes IL post-processing plus a
Roslyn analyzer/source-generator hook.

PolySharp can provide missing compiler support types for downlevel target
frameworks, but it does not replace Unity's parser or compiler.

## Decision

Build an independent frontend inspired by aspect-oriented metaprogramming and use
Unity-native backends:

- Mono.Cecil + `ILPostProcessor` for behavioral transformations;
- Roslyn 3.8 analyzer/source generator for diagnostics and future source-visible
  symbols.

Do not fork or replace Unity's Roslyn compiler.

## Consequences

### Positive

- Unity 2022.3 remains a supported baseline.
- No custom editor/compiler executable is required.
- Fields can remain fields, preserving normal Unity coding/serialization patterns.
- User-defined aspects can live in the same Unity project as their targets.
- The Cecil core can be tested outside Unity.

### Negative

- Full Metalama parity is incremental, not immediate.
- Features such as generalized `meta.Proceed()` need a custom template IR/inliner.
- Source-visible introduced symbols need cooperation from the Roslyn source backend.
- Some transformations cannot affect already-precompiled/unprocessed assemblies.

## Revisit condition

Reconsider a compiler fork only if Unity exposes a stable, documented compiler
replacement contract or if a critical feature proves impossible through both
supported backends.
