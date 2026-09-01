using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace UnityMeta.Weaver
{
    internal static class CecilExtensions
    {
        public static bool HasAttribute(this ICustomAttributeProvider provider, string fullName)
        {
            if (!provider.HasCustomAttributes)
            {
                return false;
            }

            foreach (CustomAttribute attribute in provider.CustomAttributes)
            {
                if (attribute.AttributeType.FullName == fullName)
                {
                    return true;
                }
            }

            return false;
        }

        public static CustomAttribute FindAttribute(this ICustomAttributeProvider provider, string fullName)
        {
            if (!provider.HasCustomAttributes)
            {
                return null;
            }

            foreach (CustomAttribute attribute in provider.CustomAttributes)
            {
                if (attribute.AttributeType.FullName == fullName)
                {
                    return attribute;
                }
            }

            return null;
        }

        public static bool IsOrDerivesFrom(TypeDefinition type, string fullName)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            TypeDefinition current = type;

            while (current != null && visited.Add(current.FullName))
            {
                if (current.FullName == fullName)
                {
                    return true;
                }

                if (current.BaseType == null)
                {
                    return false;
                }

                // Avoid resolving the external runtime assembly when the base
                // reference already gives us the answer. This also keeps the
                // standalone smoke-test weaver independent from an assembly
                // resolver for the common direct-inheritance case.
                if (current.BaseType.FullName == fullName)
                {
                    return true;
                }

                try
                {
                    current = current.BaseType.Resolve();
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        public static int GetAspectOrder(CustomAttribute attribute)
        {
            foreach (CustomAttributeNamedArgument property in attribute.Properties)
            {
                if (property.Name == "Order" && property.Argument.Value is int)
                {
                    return (int)property.Argument.Value;
                }
            }

            foreach (CustomAttributeNamedArgument field in attribute.Fields)
            {
                if (field.Name == "Order" && field.Argument.Value is int)
                {
                    return (int)field.Argument.Value;
                }
            }

            return 0;
        }

        public static int GetInt32Argument(CustomAttribute attribute, int index)
        {
            return Convert.ToInt32(attribute.ConstructorArguments[index].Value);
        }
    }
}
