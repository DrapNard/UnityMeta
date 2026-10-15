using UnityEngine;
using UnityMeta;

namespace UnityMeta.Samples.Log
{
    public sealed class LogAttribute : MethodAspectAttribute
    {
        public LogAttribute(string prefix)
        {
        }

        [BeforeTemplate]
        public static void Before(
            [AspectArgument(0)] string prefix,
            [TargetTypeName] string typeName,
            [TargetMemberName] string methodName)
        {
            Debug.Log(prefix + " -> " + typeName + "." + methodName);
        }

        [AfterTemplate]
        public static void After(
            [AspectArgument(0)] string prefix,
            [TargetMemberName] string methodName)
        {
            Debug.Log(prefix + " <- " + methodName);
        }
    }
}
