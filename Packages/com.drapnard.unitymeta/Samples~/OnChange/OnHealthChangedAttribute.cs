using UnityMeta;

/// <summary>
/// Sample field-change aspect. The framework supplies the previous and final
/// stored values and invokes the template only for a real value transition.
/// </summary>
public sealed class OnHealthChangedAttribute : FieldChangeAspectAttribute
{
    public string Channel { get; set; } = string.Empty;

    [ChangeTemplate]
    public static void Changed<T>(
        [TargetInstance] object target,
        [OldValue] T oldValue,
        [NewValue] T newValue,
        [AspectNamedArgument("Channel")] string channel)
    {
        ((OnChangeExample)target).HandleHealthChanged(oldValue, newValue, channel);
    }
}
