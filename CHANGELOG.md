# Changelog

## 0.1.0 - 2026-09-01

### Added

- Initial UnityMeta metaprogramming runtime API.
- Generic field-store aspect weaving with ordered `SetTemplate` handlers.
- Generic method `BeforeTemplate` / `AfterTemplate` weaving.
- Template bindings for values, aspect arguments, target metadata, target
  instance, target method arguments, and dynamic sibling-field values.
- Unity 2022 IL post-processor entry point using Mono.Cecil.
- Optional Roslyn 3.8 analyzer/source-generator companion.
- Clamp and logging samples.
- Unity-independent smoke-test harness.
- Architecture, authoring, compatibility, testing, limitation and roadmap docs.
