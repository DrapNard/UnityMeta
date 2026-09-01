using System;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UnityMeta.Weaver
{
    internal static class ILValueEmitter
    {
        public static void EmitAspectArgument(
            ILProcessor il,
            Instruction anchor,
            CustomAttribute aspect,
            int index)
        {
            EmitConstant(il, anchor, aspect.ConstructorArguments[index]);
        }

        public static void EmitAspectNamedArgument(
            ILProcessor il,
            Instruction anchor,
            CustomAttribute aspect,
            string name)
        {
            CustomAttributeArgument argument;
            if (!CecilExtensions.TryGetNamedArgument(aspect, name, out argument))
            {
                throw new InvalidOperationException(
                    "Aspect named argument '" + name + "' was validated but is no longer available.");
            }

            EmitConstant(il, anchor, argument);
        }

        public static void EmitConstant(ILProcessor il, Instruction anchor, CustomAttributeArgument argument)
        {
            object value = argument.Value;

            if (value == null)
            {
                il.InsertBefore(anchor, il.Create(OpCodes.Ldnull));
                return;
            }

            if (argument.Type.FullName == "System.Type" && value is TypeReference)
            {
                EmitType(il, anchor, (TypeReference)value);
                return;
            }

            ArrayType arrayType = argument.Type as ArrayType;
            if (arrayType != null && value is CustomAttributeArgument[])
            {
                EmitArray(il, anchor, arrayType, (CustomAttributeArgument[])value);
                return;
            }

            switch (argument.Type.MetadataType)
            {
                case MetadataType.Boolean:
                    il.InsertBefore(anchor, il.Create(OpCodes.Ldc_I4, (bool)value ? 1 : 0));
                    return;
                case MetadataType.Char:
                    il.InsertBefore(anchor, il.Create(OpCodes.Ldc_I4, (int)(char)value));
                    return;
                case MetadataType.SByte:
                case MetadataType.Byte:
                case MetadataType.Int16:
                case MetadataType.UInt16:
                case MetadataType.Int32:
                    il.InsertBefore(anchor, il.Create(OpCodes.Ldc_I4, Convert.ToInt32(value)));
                    return;
                case MetadataType.UInt32:
                    il.InsertBefore(anchor, il.Create(OpCodes.Ldc_I4, unchecked((int)Convert.ToUInt32(value))));
                    return;
                case MetadataType.Int64:
                    il.InsertBefore(anchor, il.Create(OpCodes.Ldc_I8, Convert.ToInt64(value)));
                    return;
                case MetadataType.UInt64:
                    il.InsertBefore(anchor, il.Create(OpCodes.Ldc_I8, unchecked((long)Convert.ToUInt64(value))));
                    return;
                case MetadataType.Single:
                    il.InsertBefore(anchor, il.Create(OpCodes.Ldc_R4, Convert.ToSingle(value)));
                    return;
                case MetadataType.Double:
                    il.InsertBefore(anchor, il.Create(OpCodes.Ldc_R8, Convert.ToDouble(value)));
                    return;
                case MetadataType.String:
                    il.InsertBefore(anchor, il.Create(OpCodes.Ldstr, (string)value));
                    return;
                default:
                    TypeDefinition resolved = null;
                    try
                    {
                        resolved = argument.Type.Resolve();
                    }
                    catch
                    {
                    }

                    if (resolved != null && resolved.IsEnum)
                    {
                        TypeReference underlying = resolved.Fields.Count > 0
                            ? FindEnumUnderlyingType(resolved)
                            : null;
                        EmitEnumConstant(il, anchor, value, underlying);
                        return;
                    }

                    throw new NotSupportedException(
                        "UnityMeta cannot emit aspect argument type '" + argument.Type.FullName + "' yet.");
            }
        }

        private static void EmitType(ILProcessor il, Instruction anchor, TypeReference type)
        {
            ModuleDefinition module = il.Body.Method.Module;
            il.InsertBefore(anchor, il.Create(OpCodes.Ldtoken, module.ImportReference(type)));

            MethodInfo method = typeof(Type).GetMethod(
                "GetTypeFromHandle",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(RuntimeTypeHandle) },
                null);

            if (method == null)
            {
                throw new InvalidOperationException("System.Type.GetTypeFromHandle could not be resolved.");
            }

            il.InsertBefore(anchor, il.Create(OpCodes.Call, module.ImportReference(method)));
        }

        private static void EmitArray(
            ILProcessor il,
            Instruction anchor,
            ArrayType arrayType,
            CustomAttributeArgument[] elements)
        {
            ModuleDefinition module = il.Body.Method.Module;
            TypeReference elementType = module.ImportReference(arrayType.ElementType);

            il.InsertBefore(anchor, il.Create(OpCodes.Ldc_I4, elements.Length));
            il.InsertBefore(anchor, il.Create(OpCodes.Newarr, elementType));

            for (int i = 0; i < elements.Length; i++)
            {
                il.InsertBefore(anchor, il.Create(OpCodes.Dup));
                il.InsertBefore(anchor, il.Create(OpCodes.Ldc_I4, i));
                EmitConstant(il, anchor, elements[i]);
                il.InsertBefore(anchor, il.Create(OpCodes.Stelem_Any, elementType));
            }
        }

        private static TypeReference FindEnumUnderlyingType(TypeDefinition enumType)
        {
            foreach (FieldDefinition field in enumType.Fields)
            {
                if (field.Name == "value__")
                {
                    return field.FieldType;
                }
            }

            return null;
        }

        private static void EmitEnumConstant(
            ILProcessor il,
            Instruction anchor,
            object value,
            TypeReference underlying)
        {
            MetadataType type = underlying == null ? MetadataType.Int32 : underlying.MetadataType;

            switch (type)
            {
                case MetadataType.Int64:
                case MetadataType.UInt64:
                    il.InsertBefore(anchor, il.Create(OpCodes.Ldc_I8, Convert.ToInt64(value)));
                    break;
                default:
                    il.InsertBefore(anchor, il.Create(OpCodes.Ldc_I4, Convert.ToInt32(value)));
                    break;
            }
        }
    }
}
