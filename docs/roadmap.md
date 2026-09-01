# Roadmap

## 0.2 - richer observable aspects and release tooling

Implemented in the current alpha:

- field-read/get templates for ordinary `ldfld`/`ldsfld`;
- field-change templates with old/new values and real-change filtering;
- return-value observation for method after templates;
- named attribute arguments;
- `System.Type` and attribute-array metadata emission;
- stronger Roslyn template diagnostics and release tracking;
- compiler-companion smoke coverage;
- real Unity 2022 integration-test fixture;
- UPM/GitHub Release/NuGet packaging automation.

Remaining before calling 0.2 stable:

- run the Unity fixture on a licensed Unity 2022.3 runner;
- harden diagnostics with source/PDB locations from weave-time failures;
- add more malformed-template/IL golden tests.

## 0.3 - template inlining and property/ref coverage

- inline eligible templates instead of direct helper calls;
- field address/ref semantics (`ldflda` and by-reference access);
- dedicated property get/set/change aspects;
- support target-instance bindings for value types where IL semantics permit it;
- broader custom-attribute metadata tests.

## 0.4 - Around aspects

- backend-neutral template IR;
- `AroundMethodAspect`;
- `meta.Proceed()`;
- return-value replacement;
- exception-safe `After`/`Finally` templates;
- constructor semantics.

## 0.5 - source-visible introduction

- Roslyn 3.8 source backend for symbols required by the same compilation;
- introduce methods/properties/interfaces;
- stable generated symbol naming;
- cross-link generated symbols with the IL backend.

## 0.6 - composition and project-wide aspects

- aspect dependencies and conflict diagnostics;
- explicit compile-time/run-time ordering model;
- type and assembly aspects;
- Fabric-like selectors by namespace/type/member predicate;
- transitive dependency/revalidation graph primitives.

## 0.7 - Unity-specific authoring

- Inspector validation hooks for constraints;
- generated/registered property drawers;
- `SerializedProperty`-aware change/validation behavior;
- nested serialized struct/object support.

## Long-term

Aim for the useful authoring power of Metalama while keeping Unity's compiler untouched.
Replacing/forking Unity Roslyn remains a non-goal unless Unity exposes a stable compiler
substitution contract.
