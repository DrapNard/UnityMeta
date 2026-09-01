using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UnityMeta.Weaver
{
    internal enum BindingKind
    {
        Value,
        AspectArgument,
        TargetMemberName,
        TargetTypeName,
        TargetInstance,
        TargetArgument,
        FieldValueFromAspectArgument
    }

    internal sealed class ParameterBinding
    {
        public BindingKind Kind;
        public int Index;
        public FieldReference Field;
    }

    internal sealed class BoundTemplate
    {
        public MethodReference Method;
        public ParameterBinding[] Bindings;
    }

    internal static class TemplateBinding
    {
        public static bool TryBindFieldSetTemplate(
            ModuleDefinition module,
            MethodDefinition template,
            CustomAttribute aspect,
            FieldReference targetField,
            FieldDefinition targetFieldDefinition,
            out BoundTemplate boundTemplate)
        {
            boundTemplate = null;

            if (!template.IsStatic || template.ReturnType.FullName != targetField.FieldType.FullName)
            {
                return false;
            }

            var bindings = new ParameterBinding[template.Parameters.Count];

            for (int i = 0; i < template.Parameters.Count; i++)
            {
                ParameterDefinition parameter = template.Parameters[i];
                ParameterBinding binding;

                if (!TryBindFieldParameter(parameter, aspect, targetField, targetFieldDefinition, out binding))
                {
                    return false;
                }

                bindings[i] = binding;
            }

            boundTemplate = new BoundTemplate
            {
                Method = module.ImportReference(template),
                Bindings = bindings
            };

            return true;
        }

        public static bool TryBindMethodTemplate(
            ModuleDefinition module,
            MethodDefinition template,
            CustomAttribute aspect,
            MethodDefinition targetMethod,
            out BoundTemplate boundTemplate)
        {
            boundTemplate = null;

            if (!template.IsStatic || template.ReturnType.MetadataType != MetadataType.Void)
            {
                return false;
            }

            var bindings = new ParameterBinding[template.Parameters.Count];

            for (int i = 0; i < template.Parameters.Count; i++)
            {
                ParameterDefinition parameter = template.Parameters[i];
                ParameterBinding binding;

                if (!TryBindMethodParameter(parameter, aspect, targetMethod, out binding))
                {
                    return false;
                }

                bindings[i] = binding;
            }

            boundTemplate = new BoundTemplate
            {
                Method = module.ImportReference(template),
                Bindings = bindings
            };

            return true;
        }

        private static bool TryBindFieldParameter(
            ParameterDefinition parameter,
            CustomAttribute aspect,
            FieldReference targetField,
            FieldDefinition targetFieldDefinition,
            out ParameterBinding binding)
        {
            binding = null;

            if (parameter.HasAttribute(MetaNames.Value))
            {
                if (parameter.ParameterType.FullName != targetField.FieldType.FullName)
                {
                    return false;
                }

                binding = new ParameterBinding { Kind = BindingKind.Value };
                return true;
            }

            CustomAttribute aspectArgument = parameter.FindAttribute(MetaNames.AspectArgument);
            if (aspectArgument != null)
            {
                int index = CecilExtensions.GetInt32Argument(aspectArgument, 0);
                if (!CanEmitAspectArgument(aspect, index, parameter.ParameterType))
                {
                    return false;
                }

                binding = new ParameterBinding { Kind = BindingKind.AspectArgument, Index = index };
                return true;
            }

            if (parameter.HasAttribute(MetaNames.TargetMemberName))
            {
                if (parameter.ParameterType.MetadataType != MetadataType.String)
                {
                    return false;
                }

                binding = new ParameterBinding { Kind = BindingKind.TargetMemberName };
                return true;
            }

            if (parameter.HasAttribute(MetaNames.TargetTypeName))
            {
                if (parameter.ParameterType.MetadataType != MetadataType.String)
                {
                    return false;
                }

                binding = new ParameterBinding { Kind = BindingKind.TargetTypeName };
                return true;
            }

            if (parameter.HasAttribute(MetaNames.TargetInstance))
            {
                if (targetFieldDefinition.IsStatic ||
                    parameter.ParameterType.FullName != targetFieldDefinition.DeclaringType.FullName)
                {
                    return false;
                }

                binding = new ParameterBinding { Kind = BindingKind.TargetInstance };
                return true;
            }

            CustomAttribute fieldFromArgument = parameter.FindAttribute(MetaNames.FieldValueFromAspectArgument);
            if (fieldFromArgument != null)
            {
                int index = CecilExtensions.GetInt32Argument(fieldFromArgument, 0);
                if (index < 0 || index >= aspect.ConstructorArguments.Count)
                {
                    return false;
                }

                object nameValue = aspect.ConstructorArguments[index].Value;
                string fieldName = nameValue as string;
                if (string.IsNullOrEmpty(fieldName))
                {
                    return false;
                }

                FieldDefinition sibling = FindField(targetFieldDefinition.DeclaringType, fieldName);
                if (sibling == null || sibling.FieldType.FullName != parameter.ParameterType.FullName)
                {
                    return false;
                }

                if (!sibling.IsStatic && targetFieldDefinition.IsStatic)
                {
                    return false;
                }

                binding = new ParameterBinding
                {
                    Kind = BindingKind.FieldValueFromAspectArgument,
                    Index = index,
                    Field = targetFieldDefinition.Module.ImportReference(sibling)
                };
                return true;
            }

            return false;
        }

        private static bool TryBindMethodParameter(
            ParameterDefinition parameter,
            CustomAttribute aspect,
            MethodDefinition targetMethod,
            out ParameterBinding binding)
        {
            binding = null;

            CustomAttribute aspectArgument = parameter.FindAttribute(MetaNames.AspectArgument);
            if (aspectArgument != null)
            {
                int index = CecilExtensions.GetInt32Argument(aspectArgument, 0);
                if (!CanEmitAspectArgument(aspect, index, parameter.ParameterType))
                {
                    return false;
                }

                binding = new ParameterBinding { Kind = BindingKind.AspectArgument, Index = index };
                return true;
            }

            if (parameter.HasAttribute(MetaNames.TargetMemberName))
            {
                if (parameter.ParameterType.MetadataType != MetadataType.String)
                {
                    return false;
                }

                binding = new ParameterBinding { Kind = BindingKind.TargetMemberName };
                return true;
            }

            if (parameter.HasAttribute(MetaNames.TargetTypeName))
            {
                if (parameter.ParameterType.MetadataType != MetadataType.String)
                {
                    return false;
                }

                binding = new ParameterBinding { Kind = BindingKind.TargetTypeName };
                return true;
            }

            if (parameter.HasAttribute(MetaNames.TargetInstance))
            {
                if (targetMethod.IsStatic || targetMethod.DeclaringType.IsValueType ||
                    parameter.ParameterType.FullName != targetMethod.DeclaringType.FullName)
                {
                    return false;
                }

                binding = new ParameterBinding { Kind = BindingKind.TargetInstance };
                return true;
            }

            CustomAttribute targetArgument = parameter.FindAttribute(MetaNames.TargetArgument);
            if (targetArgument != null)
            {
                int index = CecilExtensions.GetInt32Argument(targetArgument, 0);
                if (index < 0 || index >= targetMethod.Parameters.Count ||
                    targetMethod.Parameters[index].ParameterType.FullName != parameter.ParameterType.FullName)
                {
                    return false;
                }

                binding = new ParameterBinding { Kind = BindingKind.TargetArgument, Index = index };
                return true;
            }

            return false;
        }

        private static FieldDefinition FindField(TypeDefinition type, string name)
        {
            TypeDefinition current = type;
            while (current != null)
            {
                foreach (FieldDefinition field in current.Fields)
                {
                    if (field.Name == name)
                    {
                        return field;
                    }
                }

                if (current.BaseType == null)
                {
                    return null;
                }

                try
                {
                    current = current.BaseType.Resolve();
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        private static bool CanEmitAspectArgument(CustomAttribute aspect, int index, TypeReference targetType)
        {
            if (index < 0 || index >= aspect.ConstructorArguments.Count)
            {
                return false;
            }

            CustomAttributeArgument argument = aspect.ConstructorArguments[index];
            if (argument.Value == null)
            {
                return targetType.MetadataType == MetadataType.String || !targetType.IsValueType;
            }

            if (argument.Type.FullName == targetType.FullName)
            {
                return true;
            }

            // Enums are represented by their underlying constant on the IL stack.
            try
            {
                TypeDefinition resolved = targetType.Resolve();
                if (resolved != null && resolved.IsEnum)
                {
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }
    }
}
