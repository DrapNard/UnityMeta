using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UnityMeta.Weaver
{
    internal sealed class FieldStoreWeaver
    {
        private readonly ModuleDefinition _module;
        private readonly IMetaLogger _logger;

        public FieldStoreWeaver(ModuleDefinition module, IMetaLogger logger)
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
                if (instruction.OpCode != OpCodes.Stfld && instruction.OpCode != OpCodes.Stsfld)
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

                List<AspectUse> aspects = AspectDiscovery.GetFieldSetAspects(field);
                if (aspects.Count == 0)
                {
                    continue;
                }

                if (RewriteStore(method, instruction, fieldReference, field, aspects))
                {
                    modified = true;
                }
            }

            return modified;
        }

        private bool RewriteStore(
            MethodDefinition method,
            Instruction store,
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
                        "No compatible [SetTemplate] found for aspect '" + aspect.AspectType.FullName +
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
            if (!field.IsStatic)
            {
                instanceLocal = new VariableDefinition(_module.ImportReference(fieldReference.DeclaringType));
                body.Variables.Add(instanceLocal);
            }

            // Reuse the original instruction as the first operation so any
            // branch targeting the old stfld/stsfld still enters the transform.
            store.OpCode = OpCodes.Stloc;
            store.Operand = valueLocal;

            Instruction anchor = store.Next;
            if (anchor == null)
            {
                anchor = il.Create(OpCodes.Nop);
                il.Append(anchor);
            }

            if (instanceLocal != null)
            {
                il.InsertBefore(anchor, il.Create(OpCodes.Stloc, instanceLocal));
            }

            foreach (Tuple<AspectUse, BoundTemplate> item in templates)
            {
                EmitFieldTemplateCall(
                    il,
                    anchor,
                    item.Item1,
                    item.Item2,
                    field,
                    valueLocal,
                    instanceLocal);
                il.InsertBefore(anchor, il.Create(OpCodes.Stloc, valueLocal));
            }

            if (instanceLocal != null)
            {
                il.InsertBefore(anchor, il.Create(OpCodes.Ldloc, instanceLocal));
            }

            il.InsertBefore(anchor, il.Create(OpCodes.Ldloc, valueLocal));
            il.InsertBefore(
                anchor,
                il.Create(field.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, _module.ImportReference(fieldReference)));

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
                if (!method.HasAttribute(MetaNames.SetTemplate))
                {
                    continue;
                }

                BoundTemplate candidate;
                if (!TemplateBinding.TryBindFieldSetTemplate(
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
                        "Aspect '" + aspect.AspectType.FullName + "' has more than one compatible [SetTemplate] " +
                        "for field '" + field.FullName + "'.");
                    selected = null;
                    return false;
                }

                selected = candidate;
            }

            return selected != null;
        }

        private void EmitFieldTemplateCall(
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
                            "Binding '" + binding.Kind + "' is not valid for field templates.");
                }
            }

            il.InsertBefore(anchor, il.Create(OpCodes.Call, template.Method));
        }
    }
}
