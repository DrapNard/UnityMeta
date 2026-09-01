using System;

namespace UnityMeta
{
    /// <summary>
    /// Marks a static method as a field-set transformation template.
    /// The method must return the field type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class SetTemplateAttribute : Attribute
    {
    }

    /// <summary>
    /// Marks a static void method to run at method entry.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class BeforeTemplateAttribute : Attribute
    {
    }

    /// <summary>
    /// Marks a static void method to run immediately before each return.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class AfterTemplateAttribute : Attribute
    {
    }

    /// <summary>
    /// Reserved marker for future generalized compile-time templates.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TemplateAttribute : Attribute
    {
    }

    /// <summary>
    /// Reserved marker identifying values that belong to compile-time metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class CompileTimeAttribute : Attribute
    {
    }
}
