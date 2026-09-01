# Aspect authoring

UnityMeta aspects are ordinary C# attributes defined by the consuming Unity
project or another package.

## Field-set aspects

Derive from `FieldSetAspectAttribute` and add one or more static methods marked
`[SetTemplate]`.

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

Usage:

```csharp
[Positive]
public int money;
```

A set template must be `static` and return the target field type. UnityMeta tries
all templates on the aspect and selects exactly one whose bindings are compatible
with the target field and aspect constructor metadata.

### Field template parameter bindings

- `[Value] T` - value being assigned; `T` must equal the field type.
- `[AspectArgument(n)] T` - nth aspect constructor argument.
- `[TargetMemberName] string` - target field name.
- `[TargetTypeName] string` - declaring type full name.
- `[TargetInstance] T` - current target instance; currently requires the exact
  declaring reference type.
- `[FieldValueFromAspectArgument(n)] T` - argument `n` must be a string naming a
  sibling field. The sibling field is loaded directly in IL.

### Overload-by-bindability

Templates can specialize for different constructor overloads:

```csharp
[SetTemplate]
public static int Constant(
    [Value] int value,
    [AspectArgument(0)] int min,
    [AspectArgument(1)] int max) { ... }

[SetTemplate]
public static int Dynamic(
    [Value] int value,
    [AspectArgument(0)] int min,
    [FieldValueFromAspectArgument(1)] int max) { ... }
```

If constructor argument 1 is an `int`, only `Constant` binds. If it is a string
from `nameof(maxField)`, only `Dynamic` binds.

## Method aspects

Derive from `MethodAspectAttribute` and provide static void templates.

```csharp
public sealed class TraceAttribute : MethodAspectAttribute
{
    [BeforeTemplate]
    public static void Enter([TargetMemberName] string name) { ... }

    [AfterTemplate]
    public static void Exit([TargetMemberName] string name) { ... }
}
```

### Method template bindings

- `[AspectArgument(n)] T`
- `[TargetMemberName] string`
- `[TargetTypeName] string`
- `[TargetInstance] T`
- `[TargetArgument(n)] T`

`BeforeTemplate` runs at entry. `AfterTemplate` runs before every `ret`.

## Ordering

All aspects inherit `Order`:

```csharp
[First(Order = -100)]
[Second(Order = 100)]
public int value;
```

Field transformations and before templates run low-to-high. After templates run
high-to-low to preserve nesting intuition.

## Runtime cost in v0.1

Templates are normal static methods and the weaver injects a direct `call`.
There is no reflection and no aspect-object construction at runtime.

A future backend will inline eligible templates to remove that call while keeping
the authoring API stable.
