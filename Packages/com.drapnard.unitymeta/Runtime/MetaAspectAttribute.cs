using System;

namespace UnityMeta
{
    /// <summary>
    /// Base class for user-authored metaprogramming attributes.
    /// </summary>
    public abstract class MetaAspectAttribute : Attribute
    {
        /// <summary>
        /// Controls composition order when several aspects target the same member.
        /// Lower orders are applied first for field transformations and before
        /// templates. After templates run in reverse order.
        /// </summary>
        public int Order { get; set; }
    }

    /// <summary>
    /// Base class for an aspect that transforms writes to a field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public abstract class FieldSetAspectAttribute : MetaAspectAttribute
    {
    }

    /// <summary>
    /// Base class for an aspect that injects code around a method boundary.
    /// The current backend supports before and after templates.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class MethodAspectAttribute : MetaAspectAttribute
    {
    }
}
