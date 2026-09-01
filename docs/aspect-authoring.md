# Aspect authoring

UnityMeta aspects are ordinary attributes authored by a Unity project or another package.
The current backend emits direct calls to template methods, so templates must be
**public static** methods.

## Field-set aspects

Derive from `FieldSetAspectAttribute` and provide one or more `[SetTemplate]` methods.
A set template returns the target field type.

```csharp
public sealed class PositiveAttribute : FieldSetAspectAttribute
{
    [SetTemplate]
    public static int Apply([Value] int value)
    {
        return value < 0 ? 0 : value;
    }
}
```

### Field-set bindings

- `[Value] T` - value being assigned; `T` equals the target field type.
- `[AspectArgument(n)] T` - positional aspect constructor metadata.
- `[AspectNamedArgument("Name")] T` - explicitly supplied named property/field metadata.
- `[TargetMemberName] string` - target field name.
- `[TargetTypeName] string` - declaring type full name.
- `[TargetInstance] T` - current instance for reference-type declaring fields.
- `[FieldValueFromAspectArgument(n)] T` - constructor argument `n` is a sibling field name;
  the weaver emits a direct `ldfld`/`ldsfld`.

Templates can specialize by bindability. This is how a clamp can expose constant and
`nameof(maxField)` constructor overloads with separate templates.

## Field-get aspects

Derive from `FieldGetAspectAttribute` and provide one or more `[GetTemplate]` methods.
A get template receives the raw loaded field value and returns the value visible to the caller.
The underlying field storage is not modified.

```csharp
public sealed class ReadOffsetAttribute : FieldGetAspectAttribute
{
    public ReadOffsetAttribute(int offset) { }

    [GetTemplate]
    public static int Apply(
        [Value] int value,
        [AspectArgument(0)] int offset)
    {
        return value + offset;
    }
}
```

```csharp
[ReadOffset(5)]
public int displayedScore;
```

A raw stored value of `10` is observed as `15` through ordinary `ldfld`/`ldsfld` reads.
`ldflda`/address-taking (`ref` access) is intentionally not rewritten in the current alpha.
Field-get templates support `[Value]`, aspect metadata, target name/type/instance, and
`[FieldValueFromAspectArgument]` bindings.

## Field-change aspects / OnChange

Derive from `FieldChangeAspectAttribute` and provide exactly one compatible
`[ChangeTemplate]` for a target field.

```csharp
public sealed class HealthChangedAttribute : FieldChangeAspectAttribute
{
    [ChangeTemplate]
    public static void Changed(
        [TargetInstance] Combat target,
        [OldValue] int oldValue,
        [NewValue] int newValue)
    {
        target.OnHealthChanged(oldValue, newValue);
    }
}
```

Change templates:

- run **after** field-set transformations and the final store;
- receive the value that existed before the write through `[OldValue]`;
- receive the final transformed/stored value through `[NewValue]`;
- run only when `EqualityComparer<T>.Default.Equals(old, new)` is false;
- can also use aspect metadata, target name/type/instance, and dynamic sibling-field
  bindings.

This primitive is suitable for UI refresh, dirty flags, cache invalidation, local events,
replication markers, validation follow-ups, or a project-defined `OnChange` system.

## Method aspects

Derive from `MethodAspectAttribute` and provide public static void templates.

```csharp
public sealed class TraceAttribute : MethodAspectAttribute
{
    [BeforeTemplate]
    public static void Enter([TargetMemberName] string name) { }

    [AfterTemplate]
    public static void Exit(
        [TargetMemberName] string name,
        [ReturnValue] int result) { }
}
```

### Method bindings

- `[AspectArgument(n)] T`
- `[AspectNamedArgument("Name")] T`
- `[TargetMemberName] string`
- `[TargetTypeName] string`
- `[TargetInstance] T`
- `[TargetArgument(n)] T`
- `[ReturnValue] T` - only on `[AfterTemplate]`, only for non-void, non-byref returns.

`AfterTemplate` currently runs before normal `ret` instructions. It is not yet an
exception-safe `finally` hook.

## Attribute metadata supported by the call backend

Constructor/named metadata currently supports primitives, strings, enums, `System.Type`
(`typeof(...)`) and one-dimensional attribute arrays of supported element types.

A named argument is only present in attribute metadata when the use site explicitly sets
it. A template binding to `[AspectNamedArgument("X")]` therefore only matches uses that
actually specify `X`.

## Ordering and composition

All aspects inherit `Order`:

```csharp
[Normalize(Order = -100)]
[TrackChange(Order = 100)]
public int value;
```

Field-get and field-set transformations run low-to-high. Field-change templates run
low-to-high after the final store. Method before templates run low-to-high; after templates run high-to-low.

## Analyzer rules

The Roslyn companion reports `UMETA001`-`UMETA009` for invalid template shape, role,
bindings, base aspect type, generic templates, or non-public template methods. Runtime
bindability against each concrete target is still validated by the IL weaver.

## Runtime cost

The current backend injects direct static `call` instructions. There is no reflection and
no aspect-object construction at runtime. `FieldChangeAspectAttribute` uses
`EqualityComparer<T>.Default` to suppress non-changes. Template inlining is planned so the
authoring API does not need to change when call overhead is removed.
