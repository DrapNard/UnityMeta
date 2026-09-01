using System.Collections.Generic;
using System.ComponentModel;

namespace UnityMeta
{
    /// <summary>
    /// Runtime helpers used by IL emitted by UnityMeta.
    /// These members are public because woven user assemblies call them directly,
    /// but they are infrastructure rather than authoring API.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class MetaRuntimeServices
    {
        /// <summary>
        /// Compares two values with the same semantics used by
        /// <see cref="EqualityComparer{T}.Default"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool AreEqual<T>(T left, T right)
        {
            return EqualityComparer<T>.Default.Equals(left, right);
        }
    }
}
