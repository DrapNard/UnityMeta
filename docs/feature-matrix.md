# Feature matrix

| Capability | 0.2 alpha status | Backend |
| --- | --- | --- |
| User-defined field-write aspects | Implemented | Cecil ILPP |
| User-defined field-read transformations | Implemented | Cecil ILPP (`ldfld`/`ldsfld`) |
| User-defined field-change / OnChange primitives | Implemented | Cecil ILPP |
| Old/final-new value binding | Implemented | Cecil ILPP |
| Real-change filtering with `EqualityComparer<T>` | Implemented | Cecil ILPP |
| User-defined method before/after aspects | Implemented | Cecil ILPP |
| After-template return-value observation | Implemented | Cecil ILPP |
| Multiple ordered aspects | Implemented | Cecil ILPP |
| Positional aspect argument binding | Implemented | Cecil ILPP |
| Named aspect argument binding | Implemented | Cecil ILPP |
| Primitive/string/enum metadata | Implemented | Cecil ILPP |
| `typeof(...)` metadata | Implemented | Cecil ILPP |
| Attribute array metadata | Implemented | Cecil ILPP |
| `nameof` sibling field value binding | Implemented | Cecil ILPP |
| Target member/type name binding | Implemented | Cecil ILPP |
| Target instance binding | Implemented with restrictions | Cecil ILPP |
| Target method argument binding | Implemented | Cecil ILPP |
| Template diagnostics in Rider/Unity | Implemented initial rules | Roslyn 3.8 |
| Generated aspect manifest | Implemented | Roslyn 3.8 |
| Runtime reflection-free transforms | Implemented | Cecil ILPP |
| Cross-platform standalone smoke tests | Implemented | .NET 8 CI |
| Unity 2022.3 integration fixture | Implemented, manual CI | Unity/GameCI |
| Precompiled UPM release pipeline | Implemented | GitHub Actions |
| NuGet authoring package | Implemented | GitHub Actions |
| Template call inlining | Planned | Cecil ILPP |
| Property aspects | Planned | Both |
| Around aspects / `meta.Proceed()` | Planned | Template IR + Cecil |
| Return value transformation | Planned | Template IR + Cecil |
| Exception/finally aspects | Planned | Template IR + Cecil |
| Introduce method/property/interface | Planned | Roslyn source backend |
| Type aspects | Planned | Both |
| Fabric/project-wide selectors | Planned | Roslyn + manifest |
| Dependency/revalidation graphs | Planned | Both |
| Inspector-specific validation integration | Planned | Unity Editor |
| Async/iterator state-machine weaving | Planned | Cecil |
