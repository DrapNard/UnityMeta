# Contributing

UnityMeta has two hard compatibility constraints:

- Unity 2022.3 is the baseline Unity editor.
- Public/package source must compile as C# 9.

Keep Unity-specific APIs out of `Editor/Weaver/Core`; the core is tested outside
Unity against Mono.Cecil. Unity integration belongs beside the IL post-processor.

## Design rules

- Gameplay/user aspect code must never reference Mono.Cecil.
- Prefer declarative binding attributes over reflection at runtime.
- New weaving features need a smoke test that compiles a fixture, weaves it, then
  executes the transformed assembly.
- Report unsupported metacode at compile/weave time instead of silently doing
  something different.
- Preserve ordinary Unity serialization semantics whenever possible; fields
  should stay fields.
- Avoid runtime allocation/reflection in generated paths when a value can be
  resolved by the weaver.

## Checks

```bash
./build.sh
```

For Unity-specific integration, also follow `docs/testing.md` with Unity 2022.3.
