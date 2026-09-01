# Changelog

## 0.2.0-alpha.5 - 2026-09-01

### Fixed

- Normalize Cecil method-body macros before weaving and optimize them again afterwards, so
  short Roslyn branches (`br.s`, `leave.s`, and related forms) are widened when injected
  aspect IL pushes their targets outside the signed-byte range. This prevents malformed IL
  in large methods and compiler-generated state machines such as async/coroutine `MoveNext`.
- Added smoke regressions for large short-branch expansion and compiler-generated async `MoveNext` state machines.

## 0.2.0-alpha.4 - 2026-09-01

### Fixed

- Removed `.meta` files from `Samples~` and `Documentation~`; Unity intentionally excludes tilde-suffixed package trees from the AssetDatabase and does not track them with metadata.
- Set the `Unity.DrapNard.UnityMeta.CodeGen` assembly to `autoReferenced: false`, as required for Unity code-generation/IL post-processor assemblies.
- Package validation now rejects `.meta` files in ignored tilde trees and rejects auto-referenced CodeGen assemblies.

## 0.2.0-alpha.3 - 2026-09-01

### Fixed

- Git/UPM packages now ship stable Unity `.meta` files for every asset and folder. Unity
  2022.3 treats Git package caches as immutable and ignores assets whose metadata is
  missing, which previously made the package appear empty and prevented `UnityMeta.Runtime`
  from compiling.
- The Roslyn companion DLL is now imported strictly as a `RoslynAnalyzer`: normal plugin
  platforms are disabled and PluginImporter reference validation is disabled so Unity does
  not try to resolve compiler-host dependencies such as `Microsoft.CodeAnalysis` as game
  assemblies.
- Package validation now rejects releases with missing `.meta` files or an analyzer DLL
  accidentally enabled as a normal plugin.

## 0.2.0-alpha.2 - 2026-09-01

### Fixed

- Field-change equality IL now calls the C#-compiled `MetaRuntimeServices.AreEqual<T>` helper instead of hand-building a generic BCL `MemberRef`, preventing `MissingMethodException` on .NET 8 and improving runtime portability.
- Roslyn analyzer release-tracking files now use the canonical format expected by RS2007/RS2008.

## 0.2.0-alpha.1 - 2026-09-01

### Added

- Generic `FieldGetAspectAttribute` / `[GetTemplate]` read transformations for ordinary
  `ldfld`/`ldsfld` field loads without mutating storage.
- Generic `FieldChangeAspectAttribute` and `[ChangeTemplate]` authoring model.
- `[OldValue]` / `[NewValue]` bindings with `EqualityComparer<T>.Default` real-change
  filtering after final field-set transformations.
- `[ReturnValue]` observation for method after templates.
- `[AspectNamedArgument]` binding for explicitly supplied named attribute metadata.
- `System.Type` and one-dimensional attribute-array metadata emission in the IL backend.
- Stronger Roslyn diagnostics (`UMETA001` through `UMETA009`) and analyzer release tracking.
- Roslyn generator/analyzer coverage in the standalone smoke suite.
- Real Unity 2022.3.54f1 EditMode integration-test fixture and manual GameCI workflow.
- Release automation for GitHub Releases, precompiled UPM branch/tags, checksums and
  `UnityMeta.Authoring` NuGet packages.
- OnChange sample demonstrating project-defined change metacode.

### Changed

- Template methods are explicitly required to be public while the direct-call backend is
  active, ensuring aspects remain safe across assembly boundaries.
- Documentation now distinguishes source tags, precompiled `upm-v*` tags and NuGet authoring
  packages.

### Fixed

- Target-instance field binding now rejects value-type declaring fields instead of emitting
  invalid IL with the current call backend.
- Roslyn nullable warnings in the aspect-manifest generator.

## 0.1.0 - 2026-09-01

### Added

- Initial UnityMeta metaprogramming runtime API.
- Generic field-store aspect weaving with ordered `SetTemplate` handlers.
- Generic method `BeforeTemplate` / `AfterTemplate` weaving.
- Template bindings for values, aspect arguments, target metadata, target instance, target
  method arguments, and dynamic sibling-field values.
- Unity 2022 IL post-processor entry point using Mono.Cecil.
- Optional Roslyn 3.8 analyzer/source-generator companion.
- Clamp and logging samples.
- Unity-independent smoke-test harness.
- Architecture, authoring, compatibility, testing, limitation and roadmap docs.
