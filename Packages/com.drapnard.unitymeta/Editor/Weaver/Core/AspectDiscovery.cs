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
    }

    internal static class AspectDiscovery
    {
        public static List<AspectUse> GetFieldSetAspects(FieldDefinition field)
        {
            return GetAspects(field, MetaNames.FieldSetAspect);
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

            foreach (CustomAttribute attribute in provider.CustomAttributes)
            {
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
                    Order = CecilExtensions.GetAspectOrder(attribute)
                });
            }

            result.Sort(delegate(AspectUse left, AspectUse right)
            {
                return left.Order.CompareTo(right.Order);
            });

            return result;
        }
    }
}
