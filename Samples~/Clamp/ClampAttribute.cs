using UnityMeta;

namespace UnityMeta.Samples.Clamp
{
    /// <summary>
    /// Example aspect. Clamp is deliberately a sample, not a built-in UnityMeta feature.
    /// </summary>
    public sealed class ClampAttribute : FieldSetAspectAttribute
    {
        public ClampAttribute(int min, int max)
        {
        }

        public ClampAttribute(int min, string maxField)
        {
        }

        public ClampAttribute(float min, float max)
        {
        }

        public ClampAttribute(float min, string maxField)
        {
        }

        [SetTemplate]
        public static int IntConstant(
            [Value] int value,
            [AspectArgument(0)] int min,
            [AspectArgument(1)] int max)
        {
            return value < min ? min : value > max ? max : value;
        }

        [SetTemplate]
        public static int IntDynamicMax(
            [Value] int value,
            [AspectArgument(0)] int min,
            [FieldValueFromAspectArgument(1)] int max)
        {
            return value < min ? min : value > max ? max : value;
        }

        [SetTemplate]
        public static float FloatConstant(
            [Value] float value,
            [AspectArgument(0)] float min,
            [AspectArgument(1)] float max)
        {
            return value < min ? min : value > max ? max : value;
        }

        [SetTemplate]
        public static float FloatDynamicMax(
            [Value] float value,
            [AspectArgument(0)] float min,
            [FieldValueFromAspectArgument(1)] float max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
