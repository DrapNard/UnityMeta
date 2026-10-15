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
        /// Lower orders are applied first for transformations and notifications.
        /// Method after templates run in reverse order.
        /// </summary>
        public int Order { get; set; }
    }

    /// <summary>
    /// Base class for an aspect that transforms writes to a field before storage.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public abstract class FieldSetAspectAttribute : MetaAspectAttribute
    {
    }

    /// <summary>
    /// Base class for an aspect that transforms values loaded from a field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public abstract class FieldGetAspectAttribute : MetaAspectAttribute
    {
    }

    /// <summary>
    /// Base class for an aspect that observes a field after a successful write.
    /// Change templates execute only when the final stored value differs from the
    /// previous value according to <see cref="System.Collections.Generic.EqualityComparer{T}.Default"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public abstract class FieldChangeAspectAttribute : MetaAspectAttribute
    {
    }

    /// <summary>
    /// Base class for an aspect that injects code around a method boundary.
    /// The current backend supports before and normal-return after templates.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class MethodAspectAttribute : MetaAspectAttribute
    {
    }
}
