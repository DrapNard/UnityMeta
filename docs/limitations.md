# Known limitations

UnityMeta 0.2 alpha is a usable architecture preview, not Metalama feature parity.

## Current limitations

- No generalized `Around` template or `meta.Proceed()` yet.
- No source-visible member/type/interface introduction yet.
- Templates are direct public static calls and are not inlined yet.
- Direct `ldfld`/`ldsfld` reads can be intercepted with `[GetTemplate]`; address-taking
  (`ldflda`, `ref`/by-reference field access) is not rewritten yet.
- Properties do not yet have a dedicated property-aspect model.
- Writes from an unprocessed/precompiled assembly cannot be retroactively transformed.
- `TargetInstance` does not currently bind value-type declaring instances.
- Dynamic field binding supports fields, not properties/indexers.
- Named aspect arguments must be explicitly supplied at the attribute use site.
- Attribute metadata does not yet cover every legal CLR custom-attribute edge case.
- Generic template methods are intentionally rejected by the current backend.
- No dependency/revalidation graph yet. A dynamic bound changing does not automatically
  reassign every dependent field; change aspects can be used to build project-level behavior
  until dependency primitives land.
- Async/iterator aspects do not yet target state-machine bodies with Metalama-like semantics.
- `AfterTemplate` handles normal returns only, not exceptions/finally.
- Return values can be observed but not replaced yet.
- Unity Inspector serialization can bypass gameplay write instructions. Inspector-specific
  aspect hooks/property drawers are future Editor integration work.

## Compatibility promise

The base aspect classes and binding concepts are intended to stay recognizable, but all
0.x releases are pre-1.0 and may receive API refinements when `Around`, inlining, member
introduction and project-wide selectors are implemented.
