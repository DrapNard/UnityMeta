using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UnityMeta.Weaver
{
    internal sealed class MethodAspectWeaver
    {
        private readonly ModuleDefinition _module;
        private readonly IMetaLogger _logger;

        public MethodAspectWeaver(ModuleDefinition module, IMetaLogger logger)
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

            List<AspectUse> aspects = AspectDiscovery.GetMethodAspects(method);
            if (aspects.Count == 0)
            {
                return false;
            }

            var before = new List<Tuple<AspectUse, BoundTemplate>>();
            var after = new List<Tuple<AspectUse, BoundTemplate>>();

            foreach (AspectUse aspect in aspects)
            {
                foreach (MethodDefinition templateMethod in aspect.AspectType.Methods)
                {
                    bool isBefore = templateMethod.HasAttribute(MetaNames.BeforeTemplate);
                    bool isAfter = templateMethod.HasAttribute(MetaNames.AfterTemplate);
                    if (!isBefore && !isAfter)
                    {
                        continue;
                    }

                    BoundTemplate template;
                    if (!TemplateBinding.TryBindMethodTemplate(
                            _module,
                            templateMethod,
                            aspect.Attribute,
                            method,
                            out template))
                    {
                        _logger.Error(
                            "Method template '" + templateMethod.FullName + "' cannot be bound to '" + method.FullName + "'.");
                        return false;
                    }

                    if (isBefore)
                    {
                        before.Add(Tuple.Create(aspect, template));
                    }
                    if (isAfter)
                    {
                        after.Add(Tuple.Create(aspect, template));
                    }
                }
            }

            ILProcessor il = method.Body.GetILProcessor();
            Instruction first = method.Body.Instructions[0];

            foreach (Tuple<AspectUse, BoundTemplate> item in before)
            {
                EmitMethodTemplateCall(il, first, item.Item1, item.Item2, method);
            }

            if (after.Count > 0)
            {
                Instruction[] snapshot = new Instruction[method.Body.Instructions.Count];
                method.Body.Instructions.CopyTo(snapshot, 0);

                foreach (Instruction instruction in snapshot)
                {
                    if (instruction.OpCode != OpCodes.Ret)
                    {
                        continue;
                    }

                    for (int i = after.Count - 1; i >= 0; i--)
                    {
                        EmitMethodTemplateCall(il, instruction, after[i].Item1, after[i].Item2, method);
                    }
                }
            }

            return before.Count > 0 || after.Count > 0;
        }

        private void EmitMethodTemplateCall(
            ILProcessor il,
            Instruction anchor,
            AspectUse aspect,
            BoundTemplate template,
            MethodDefinition targetMethod)
        {
            foreach (ParameterBinding binding in template.Bindings)
            {
                switch (binding.Kind)
                {
                    case BindingKind.AspectArgument:
                        ILValueEmitter.EmitAspectArgument(il, anchor, aspect.Attribute, binding.Index);
                        break;
                    case BindingKind.TargetMemberName:
                        il.InsertBefore(anchor, il.Create(OpCodes.Ldstr, targetMethod.Name));
                        break;
                    case BindingKind.TargetTypeName:
                        il.InsertBefore(anchor, il.Create(OpCodes.Ldstr, targetMethod.DeclaringType.FullName));
                        break;
                    case BindingKind.TargetInstance:
                        il.InsertBefore(anchor, il.Create(OpCodes.Ldarg_0));
                        break;
                    case BindingKind.TargetArgument:
                        EmitLoadTargetArgument(il, anchor, targetMethod, binding.Index);
                        break;
                    default:
                        throw new NotSupportedException(
                            "Binding '" + binding.Kind + "' is not valid for method templates.");
                }
            }

            il.InsertBefore(anchor, il.Create(OpCodes.Call, template.Method));
        }

        private static void EmitLoadTargetArgument(
            ILProcessor il,
            Instruction anchor,
            MethodDefinition targetMethod,
            int parameterIndex)
        {
            int ilIndex = targetMethod.IsStatic ? parameterIndex : parameterIndex + 1;

            switch (ilIndex)
            {
                case 0:
                    il.InsertBefore(anchor, il.Create(OpCodes.Ldarg_0));
                    break;
                case 1:
                    il.InsertBefore(anchor, il.Create(OpCodes.Ldarg_1));
                    break;
                case 2:
                    il.InsertBefore(anchor, il.Create(OpCodes.Ldarg_2));
                    break;
                case 3:
                    il.InsertBefore(anchor, il.Create(OpCodes.Ldarg_3));
                    break;
                default:
                    il.InsertBefore(anchor, il.Create(OpCodes.Ldarg, targetMethod.Parameters[parameterIndex]));
                    break;
            }
        }
    }
}
