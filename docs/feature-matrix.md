# Feature matrix

| Capability | 0.1 status | Backend |
| --- | --- | --- |
| User-defined field-write aspects | Implemented | Cecil ILPP |
| User-defined method before/after aspects | Implemented | Cecil ILPP |
| Multiple ordered aspects | Implemented | Cecil ILPP |
| Aspect constructor argument binding | Implemented for primitive/string/enum | Cecil ILPP |
| `nameof` sibling field value binding | Implemented | Cecil ILPP |
| Target member/type name binding | Implemented | Cecil ILPP |
| Target instance binding | Implemented with current restrictions | Cecil ILPP |
| Target method argument binding | Implemented | Cecil ILPP |
| Template diagnostics in Rider/Unity | Initial analyzer | Roslyn 3.8 |
| Generated aspect manifest | Implemented | Roslyn 3.8 |
| Runtime reflection-free field transforms | Implemented | Cecil ILPP |
| Template call inlining | Planned | Cecil ILPP |
| Around aspects / `meta.Proceed()` | Planned | Template IR + Cecil |
| Return value binding | Planned | Template IR + Cecil |
| Exception/finally aspects | Planned | Template IR + Cecil |
| Introduce method/property/interface | Planned | Roslyn source backend |
| Type aspects | Planned | Both |
| Fabric/project-wide selectors | Planned | Roslyn + manifest |
| Dependency/revalidation graphs | Planned | Both |
| Inspector-specific validation integration | Planned | Unity Editor |
| Async/iterator state-machine weaving | Planned | Cecil |
