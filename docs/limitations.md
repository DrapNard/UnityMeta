# Known limitations

UnityMeta 0.1 is an MVP proving a Unity-native metaprogramming architecture.

## Current limitations

- No generalized `Around` template or `meta.Proceed()` yet.
- No source-visible member/type/interface introduction yet.
- Set templates are invoked by a direct static call; they are not inlined yet.
- Field writes can only be transformed in assemblies processed by UnityMeta.
  Writes from an unprocessed/precompiled assembly cannot be retroactively changed.
- `TargetInstance` currently targets reference-type declaring types only for
  method templates.
- Dynamic field binding supports sibling fields, not properties/indexers.
- Aspect constructor argument injection currently supports primitive/string/enum
  metadata; `System.Type`, arrays and complex metadata are planned.
- No automatic dependency graph/revalidation when a dynamic bound field changes.
  That behavior belongs in a higher-level user aspect or a future dependency API.
- Async/iterator method aspects affect the generated entry/stub method, not yet
  the state-machine body semantics expected from a full Metalama-style around
  aspect.
- Exception-safe after/finally semantics are not implemented; `AfterTemplate`
  runs before normal `ret` instructions only.
- Unity Inspector writes directly to serialized fields and do not necessarily
  execute code paths containing `stfld`. Inspector-specific validation is an
  Editor concern for the aspect author today.

## Compatibility promise

The public binding attributes and base aspect classes are intended to remain
stable. Backend internals and exact generated IL are explicitly experimental.
