using System;

namespace UnityMeta
{
    /// <summary>
    /// Binds a field-set template parameter to the value being assigned.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class ValueAttribute : Attribute
    {
    }

    /// <summary>
    /// Binds a template parameter to a positional constructor argument of the
    /// aspect instance stored in metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class AspectArgumentAttribute : Attribute
    {
        public AspectArgumentAttribute(int index)
        {
            Index = index;
        }

        public int Index { get; private set; }
    }

    /// <summary>
    /// Binds a template parameter to the target member name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class TargetMemberNameAttribute : Attribute
    {
    }

    /// <summary>
    /// Binds a template parameter to the target declaring type full name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class TargetTypeNameAttribute : Attribute
    {
    }

    /// <summary>
    /// Binds a template parameter to the target instance (`this`).
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class TargetInstanceAttribute : Attribute
    {
    }

    /// <summary>
    /// Binds a method template parameter to one argument of the target method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class TargetArgumentAttribute : Attribute
    {
        public TargetArgumentAttribute(int index)
        {
            Index = index;
        }

        public int Index { get; private set; }
    }

    /// <summary>
    /// Interprets an aspect constructor argument as the name of a sibling field
    /// and loads that field directly at the woven call site. This provides
    /// dynamic bounds/configuration without runtime reflection.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class FieldValueFromAspectArgumentAttribute : Attribute
    {
        public FieldValueFromAspectArgumentAttribute(int argumentIndex)
        {
            ArgumentIndex = argumentIndex;
        }

        public int ArgumentIndex { get; private set; }
    }
}
