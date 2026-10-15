using UnityMeta;

namespace UnityMeta.Samples.ReadTransform
{
    /// <summary>
    /// Example read transform. The stored value remains unchanged; callers observe
    /// the value after this template runs.
    /// </summary>
    public sealed class ReadOffsetAttribute : FieldGetAspectAttribute
    {
        public ReadOffsetAttribute(int offset)
        {
        }

        [GetTemplate]
        public static int Apply(
            [Value] int value,
            [AspectArgument(0)] int offset)
        {
            return value + offset;
        }
    }
}
