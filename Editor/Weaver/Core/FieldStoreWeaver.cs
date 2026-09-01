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

                List<AspectUse> setAspects = AspectDiscovery.GetFieldSetAspects(field);
                List<AspectUse> changeAspects = AspectDiscovery.GetFieldChangeAspects(field);
                if (setAspects.Count == 0 && changeAspects.Count == 0)
                {
                    continue;
                }

                if (RewriteStore(method, instruction, fieldReference, field, setAspects, changeAspects))
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
            List<AspectUse> setAspects,
            List<AspectUse> changeAspects)
        {
            var setTemplates = new List<Tuple<AspectUse, BoundTemplate>>();
            var changeTemplates = new List<Tuple<AspectUse, BoundTemplate>>();

            foreach (AspectUse aspect in setAspects)
            {
                BoundTemplate template;
                if (!TrySelectSetTemplate(aspect, fieldReference, field, out template))
                {
                    _logger.Error(
                        "No compatible [SetTemplate] found for aspect '" + aspect.AspectType.FullName +
                        "' on field '" + field.FullName + "'.");
                    return false;
                }

                setTemplates.Add(Tuple.Create(aspect, template));
            }

            foreach (AspectUse aspect in changeAspects)
            {
                BoundTemplate template;
                if (!TrySelectChangeTemplate(aspect, fieldReference, field, out template))
                {
                    _logger.Error(
                        "No compatible [ChangeTemplate] found for aspect '" + aspect.AspectType.FullName +
                        "' on field '" + field.FullName + "'.");
                    return false;
                }

                changeTemplates.Add(Tuple.Create(aspect, template));
            }

            MethodBody body = method.Body;
            body.InitLocals = true;
            ILProcessor il = body.GetILProcessor();

            var valueLocal = new VariableDefinition(_module.ImportReference(fieldReference.FieldType));
            body.Variables.Add(valueLocal);

            VariableDefinition oldValueLocal = null;
            if (changeTemplates.Count > 0)
            {
                oldValueLocal = new VariableDefinition(_module.ImportReference(fieldReference.FieldType));
                body.Variables.Add(oldValueLocal);
            }

            VariableDefinition instanceLocal = null;
            if (!field.IsStatic)
            {
                TypeReference declaringType = _module.ImportReference(fieldReference.DeclaringType);
                TypeReference instanceType = field.DeclaringType.IsValueType
                    ? (TypeReference)new ByReferenceType(declaringType)
                    : declaringType;

                instanceLocal = new VariableDefinition(instanceType);
                body.Variables.Add(instanceLocal);
            }

            // Original stack is [instance?, value]. Reuse the original store
            // instruction as the first local store so any branch that targeted the
            // old stfld/stsfld still enters the complete transformation sequence.
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

            if (oldValueLocal != null)
            {
                if (field.IsStatic)
                {
                    il.InsertBefore(anchor, il.Create(OpCodes.Ldsfld, _module.ImportReference(fieldReference)));
                }
                else
                {
                    il.InsertBefore(anchor, il.Create(OpCodes.Ldloc, instanceLocal));
                    il.InsertBefore(anchor, il.Create(OpCodes.Ldfld, _module.ImportReference(fieldReference)));
                }

                il.InsertBefore(anchor, il.Create(OpCodes.Stloc, oldValueLocal));
            }

            foreach (Tuple<AspectUse, BoundTemplate> item in setTemplates)
            {
                EmitFieldTemplateCall(
                    il,
                    anchor,
                    item.Item1,
                    item.Item2,
                    field,
                    valueLocal,
                    null,
                    null,
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

            if (changeTemplates.Count > 0)
            {
                Instruction skipChangeTemplates = il.Create(OpCodes.Nop);
                EmitEqualityComparison(il, anchor, fieldReference.FieldType, oldValueLocal, valueLocal);
                il.InsertBefore(anchor, il.Create(OpCodes.Brtrue, skipChangeTemplates));

                foreach (Tuple<AspectUse, BoundTemplate> item in changeTemplates)
                {
                    EmitFieldTemplateCall(
                        il,
                        anchor,
                        item.Item1,
                        item.Item2,
                        field,
                        valueLocal,
                        oldValueLocal,
                        valueLocal,
                        instanceLocal);
                }

                il.InsertBefore(anchor, skipChangeTemplates);
            }

            return true;
        }

        private bool TrySelectSetTemplate(
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

        private bool TrySelectChangeTemplate(
            AspectUse aspect,
            FieldReference fieldReference,
            FieldDefinition field,
            out BoundTemplate selected)
        {
            selected = null;

            foreach (MethodDefinition method in aspect.AspectType.Methods)
            {
                if (!method.HasAttribute(MetaNames.ChangeTemplate))
                {
                    continue;
                }

                BoundTemplate candidate;
                if (!TemplateBinding.TryBindFieldChangeTemplate(
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
                        "Aspect '" + aspect.AspectType.FullName + "' has more than one compatible [ChangeTemplate] " +
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
            VariableDefinition oldValueLocal,
            VariableDefinition newValueLocal,
            VariableDefinition instanceLocal)
        {
            foreach (ParameterBinding binding in template.Bindings)
            {
                switch (binding.Kind)
                {
                    case BindingKind.Value:
                        il.InsertBefore(anchor, il.Create(OpCodes.Ldloc, valueLocal));
                        break;
                    case BindingKind.OldValue:
                        il.InsertBefore(anchor, il.Create(OpCodes.Ldloc, oldValueLocal));
                        break;
                    case BindingKind.NewValue:
                        il.InsertBefore(anchor, il.Create(OpCodes.Ldloc, newValueLocal));
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
                            "Binding '" + binding.Kind + "' is not valid for field templates.");
                }
            }

            il.InsertBefore(anchor, il.Create(OpCodes.Call, template.Method));
        }

        private void EmitEqualityComparison(
            ILProcessor il,
            Instruction anchor,
            TypeReference valueType,
            VariableDefinition oldValueLocal,
            VariableDefinition newValueLocal)
        {
            TypeReference importedValueType = _module.ImportReference(valueType);

            // Do not hand-craft a MemberRef to EqualityComparer<T>.Default here.
            // The exact generic member signature is runtime/BCL-sensitive and a
            // syntactically valid Cecil reference can still fail at runtime with
            // MissingMethodException. Instead, call a normal C#-compiled generic
            // runtime helper and let the C# compiler own the BCL call signature.
            var helperDefinition = typeof(UnityMeta.MetaRuntimeServices).GetMethod(
                nameof(UnityMeta.MetaRuntimeServices.AreEqual));
            MethodReference importedHelper = _module.ImportReference(helperDefinition);
            var closedHelper = new GenericInstanceMethod(importedHelper);
            closedHelper.GenericArguments.Add(importedValueType);

            il.InsertBefore(anchor, il.Create(OpCodes.Ldloc, oldValueLocal));
            il.InsertBefore(anchor, il.Create(OpCodes.Ldloc, newValueLocal));
            il.InsertBefore(anchor, il.Create(OpCodes.Call, closedHelper));
        }
    }
}
