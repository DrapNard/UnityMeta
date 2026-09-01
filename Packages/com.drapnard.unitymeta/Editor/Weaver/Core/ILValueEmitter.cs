using System;
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
            CustomAttributeArgument argument = aspect.ConstructorArguments[index];
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
                    // CustomAttributeArgument.Type can be an enum rather than its
                    // underlying primitive type.
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
