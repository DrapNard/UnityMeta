using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace UnityMeta.Weaver
{
    internal sealed class AspectUse
    {
        public CustomAttribute Attribute;
        public TypeDefinition AspectType;
        public int Order;
        public int Sequence;
    }

    internal static class AspectDiscovery
    {
        public static List<AspectUse> GetFieldSetAspects(FieldDefinition field)
        {
            return GetAspects(field, MetaNames.FieldSetAspect);
        }

        public static List<AspectUse> GetFieldGetAspects(FieldDefinition field)
        {
            return GetAspects(field, MetaNames.FieldGetAspect);
        }

        public static List<AspectUse> GetFieldChangeAspects(FieldDefinition field)
        {
            return GetAspects(field, MetaNames.FieldChangeAspect);
        }

        public static List<AspectUse> GetMethodAspects(MethodDefinition method)
        {
            return GetAspects(method, MetaNames.MethodAspect);
        }

        private static List<AspectUse> GetAspects(ICustomAttributeProvider provider, string requiredBaseType)
        {
            var result = new List<AspectUse>();

            if (!provider.HasCustomAttributes)
            {
                return result;
            }

            int sequence = 0;
            foreach (CustomAttribute attribute in provider.CustomAttributes)
            {
                int currentSequence = sequence++;
                TypeDefinition aspectType;
                try
                {
                    aspectType = attribute.AttributeType.Resolve();
                }
                catch
                {
                    continue;
                }

                if (aspectType == null || !CecilExtensions.IsOrDerivesFrom(aspectType, requiredBaseType))
                {
                    continue;
                }

                result.Add(new AspectUse
                {
                    Attribute = attribute,
                    AspectType = aspectType,
                    Order = CecilExtensions.GetAspectOrder(attribute),
                    Sequence = currentSequence
                });
            }

            result.Sort(delegate(AspectUse left, AspectUse right)
            {
                int order = left.Order.CompareTo(right.Order);
                return order != 0 ? order : left.Sequence.CompareTo(right.Sequence);
            });

            return result;
        }
    }
}
