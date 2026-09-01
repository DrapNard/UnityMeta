# Testing

## Non-Unity smoke tests

`tests/UnityMeta.SmokeTests` compiles a C# fixture with Roslyn, loads it through
Mono.Cecil, applies the real weaver core, reloads the transformed assembly and
asserts runtime behavior.

The fixture covers:

- constant field-set transformation;
- `nameof`-driven sibling-field transformation without reflection;
- before/after method templates;
- preservation of target method return values.

Run:

```bash
./build.sh
```

## Unity 2022.3 manual sanity test

1. Create/open a Unity 2022.3 project.
2. Add the UPM package from disk.
3. Import the Clamp and Log samples.
4. Enter Play Mode.
5. Confirm `CombatExample.hp = 900` is limited to `hpMax` after `Start`.
6. Confirm negative energy is limited to zero.
7. Invoke `LogExample.Attack` and confirm entry/exit messages.
8. Create a second asmdef referencing `UnityMeta.Runtime` and verify weaving also
   occurs there.
9. Add `UNITYMETA_DISABLE_WEAVING` and confirm behavior returns to normal C#.
10. Build once with Mono and once with IL2CPP to ensure the post-processed assembly
    is accepted by both backends.

## CI

GitHub Actions builds the runtime, Cecil core and Roslyn 3.8 companion, runs smoke
tests, and validates UPM metadata. Unity Editor integration still needs a Unity
runner/license and is intentionally documented as a separate validation tier.
