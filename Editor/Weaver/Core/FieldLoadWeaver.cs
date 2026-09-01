using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UnityMeta.Weaver
{
    internal sealed class FieldLoadWeaver
    {
        private readonly ModuleDefinition _module;
        private readonly IMetaLogger _logger;

        public FieldLoadWeaver(ModuleDefinition module, IMetaLogger logger)
        {
            _module = module;
            _logger = logger;
        }

        public bool Process(MethodDefinition method)
        {
            if (!method.HasBody || method.Body.Instructions.Count == 0)
            {
                return false;
            }

            bool modified = false;
            Instruction[] snapshot = new Instruction[method.Body.Instructions.Count];
            method.Body.Instructions.CopyTo(snapshot, 0);

            foreach (Instruction instruction in snapshot)
            {
                if (instruction.OpCode != OpCodes.Ldfld && instruction.OpCode != OpCodes.Ldsfld)
                {
                    continue;
                }

                FieldReference fieldReference = instruction.Operand as FieldReference;
                if (fieldReference == null)
                {
                    continue;
                }

                FieldDefinition field;
                try
                {
                    field = fieldReference.Resolve();
                }
                catch
                {
                    continue;
                }

                if (field == null)
                {
                    continue;
                }

                List<AspectUse> aspects = AspectDiscovery.GetFieldGetAspects(field);
                if (aspects.Count == 0)
                {
                    continue;
                }

                if (!field.IsStatic && HasFieldPrefix(instruction.Previous))
                {
                    _logger.Warning(
                        "Skipping [GetTemplate] weaving for prefixed instance field load '" +
                        field.FullName + "' in '" + method.FullName + "'.");
                    continue;
                }

                if (RewriteLoad(method, instruction, fieldReference, field, aspects))
                {
                    modified = true;
                }
            }

            return modified;
        }

        private bool RewriteLoad(
            MethodDefinition method,
            Instruction load,
            FieldReference fieldReference,
            FieldDefinition field,
            List<AspectUse> aspects)
        {
            var templates = new List<Tuple<AspectUse, BoundTemplate>>();

            foreach (AspectUse aspect in aspects)
            {
                BoundTemplate template;
                if (!TrySelectTemplate(aspect, fieldReference, field, out template))
                {
                    _logger.Error(
                        "No compatible [GetTemplate] found for aspect '" + aspect.AspectType.FullName +
                        "' on field '" + field.FullName + "'.");
                    return false;
                }

                templates.Add(Tuple.Create(aspect, template));
            }

            MethodBody body = method.Body;
            body.InitLocals = true;
            ILProcessor il = body.GetILProcessor();

            var valueLocal = new VariableDefinition(_module.ImportReference(fieldReference.FieldType));
            body.Variables.Add(valueLocal);

            VariableDefinition instanceLocal = null;
            Instruction anchor;

            if (field.IsStatic)
            {
                anchor = load.Next;
                if (anchor == null)
                {
                    anchor = il.Create(OpCodes.Nop);
                    il.Append(anchor);
                }

                // Keep the original ldsfld so branch/exception references remain valid.
                il.InsertBefore(anchor, il.Create(OpCodes.Stloc, valueLocal));
            }
            else
            {
                TypeReference declaringType = _module.ImportReference(fieldReference.DeclaringType);
                TypeReference instanceType = field.DeclaringType.IsValueType
                    ? (TypeReference)new ByReferenceType(declaringType)
                    : declaringType;

                instanceLocal = new VariableDefinition(instanceType);
                body.Variables.Add(instanceLocal);

                // Original stack is [instance]. Reuse the branch-targeted ldfld as
                // the instance capture, then perform the real load immediately after.
                load.OpCode = OpCodes.Stloc;
                load.Operand = instanceLocal;

                anchor = load.Next;
                if (anchor == null)
                {
                    anchor = il.Create(OpCodes.Nop);
                    il.Append(anchor);
                }

                il.InsertBefore(anchor, il.Create(OpCodes.Ldloc, instanceLocal));
                il.InsertBefore(anchor, il.Create(OpCodes.Ldfld, _module.ImportReference(fieldReference)));
                il.InsertBefore(anchor, il.Create(OpCodes.Stloc, valueLocal));
            }

            foreach (Tuple<AspectUse, BoundTemplate> item in templates)
            {
                EmitTemplateCall(
                    il,
                    anchor,
                    item.Item1,
                    item.Item2,
                    field,
                    valueLocal,
                    instanceLocal);
                il.InsertBefore(anchor, il.Create(OpCodes.Stloc, valueLocal));
            }

            il.InsertBefore(anchor, il.Create(OpCodes.Ldloc, valueLocal));
            return true;
        }

        private bool TrySelectTemplate(
            AspectUse aspect,
            FieldReference fieldReference,
            FieldDefinition field,
            out BoundTemplate selected)
        {
            selected = null;

            foreach (MethodDefinition method in aspect.AspectType.Methods)
            {
                if (!method.HasAttribute(MetaNames.GetTemplate))
                {
                    continue;
                }

                BoundTemplate candidate;
                if (!TemplateBinding.TryBindFieldGetTemplate(
                        _module,
                        method,
                        aspect.Attribute,
                        fieldReference,
                        field,
                        out candidate))
                {
                    continue;
                }

                if (selected != null)
                {
                    _logger.Error(
                        "Aspect '" + aspect.AspectType.FullName + "' has more than one compatible [GetTemplate] " +
                        "for field '" + field.FullName + "'.");
                    selected = null;
                    return false;
                }

                selected = candidate;
            }

            return selected != null;
        }

        private void EmitTemplateCall(
            ILProcessor il,
            Instruction anchor,
            AspectUse aspect,
            BoundTemplate template,
            FieldDefinition targetField,
            VariableDefinition valueLocal,
            VariableDefinition instanceLocal)
        {
            foreach (ParameterBinding binding in template.Bindings)
            {
                switch (binding.Kind)
                {
                    case BindingKind.Value:
                        il.InsertBefore(anchor, il.Create(OpCodes.Ldloc, valueLocal));
                        break;
                    case BindingKind.AspectArgument:
                        ILValueEmitter.EmitAspectArgument(il, anchor, aspect.Attribute, binding.Index);
                        break;
                    case BindingKind.AspectNamedArgument:
                        ILValueEmitter.EmitAspectNamedArgument(il, anchor, aspect.Attribute, binding.Name);
                        break;
                    case BindingKind.TargetMemberName:
                        il.InsertBefore(anchor, il.Create(OpCodes.Ldstr, targetField.Name));
                        break;
                    case BindingKind.TargetTypeName:
                        il.InsertBefore(anchor, il.Create(OpCodes.Ldstr, targetField.DeclaringType.FullName));
                        break;
                    case BindingKind.TargetInstance:
                        il.InsertBefore(anchor, il.Create(OpCodes.Ldloc, instanceLocal));
                        break;
                    case BindingKind.FieldValueFromAspectArgument:
                        if (binding.Field.Resolve().IsStatic)
                        {
                            il.InsertBefore(anchor, il.Create(OpCodes.Ldsfld, _module.ImportReference(binding.Field)));
                        }
                        else
                        {
                            il.InsertBefore(anchor, il.Create(OpCodes.Ldloc, instanceLocal));
                            il.InsertBefore(anchor, il.Create(OpCodes.Ldfld, _module.ImportReference(binding.Field)));
                        }
                        break;
                    default:
                        throw new NotSupportedException(
                            "Binding '" + binding.Kind + "' is not valid for field-get templates.");
                }
            }

            il.InsertBefore(anchor, il.Create(OpCodes.Call, template.Method));
        }

        private static bool HasFieldPrefix(Instruction instruction)
        {
            return instruction != null &&
                   (instruction.OpCode == OpCodes.Volatile || instruction.OpCode == OpCodes.Unaligned);
        }
    }
}
