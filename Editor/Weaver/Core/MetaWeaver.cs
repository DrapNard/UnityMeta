using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace UnityMeta.Weaver
{
    /// <summary>
    /// Unity-independent Cecil weaving engine.
    /// </summary>
    public sealed class MetaWeaver
    {
        private readonly IMetaLogger _logger;

        public MetaWeaver(IMetaLogger logger)
        {
            _logger = logger;
        }

        public bool Weave(AssemblyDefinition assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            bool modified = false;
            var fieldLoadWeaver = new FieldLoadWeaver(assembly.MainModule, _logger);
            var fieldWeaver = new FieldStoreWeaver(assembly.MainModule, _logger);
            var methodWeaver = new MethodAspectWeaver(assembly.MainModule, _logger);

            foreach (TypeDefinition type in EnumerateTypes(assembly.MainModule.Types))
            {
                // Do not weave template methods themselves. They are ordinary
                // runtime helper methods in the current direct-call backend and can later be inlined by a
                // more advanced backend.
                if (IsAspectType(type))
                {
                    continue;
                }

                foreach (MethodDefinition method in type.Methods)
                {
                    if (!method.HasBody)
                    {
                        continue;
                    }

                    // Roslyn emits short-form branches (br.s, leave.s, etc.) whenever the
                    // original target is within sbyte range. Aspect weaving can insert enough
                    // IL between the branch and target to push that distance past +/-127 bytes.
                    // Cecil does not implicitly widen those existing opcodes while arbitrary
                    // instructions are inserted, so normalize macros before weaving and choose
                    // the correct compact/long forms again after all transformations.
                    method.Body.SimplifyMacros();

                    bool methodModified = false;
                    try
                    {
                        // Process original field reads before stores. Store weaving injects
                        // raw field loads for old-value/dynamic metadata that must not be
                        // reinterpreted as user-level reads.
                        if (fieldLoadWeaver.Process(method))
                        {
                            methodModified = true;
                        }

                        if (fieldWeaver.Process(method))
                        {
                            methodModified = true;
                        }

                        if (methodWeaver.Process(method))
                        {
                            methodModified = true;
                        }
                    }
                    finally
                    {
                        method.Body.OptimizeMacros();
                    }

                    if (methodModified)
                    {
                        modified = true;
                    }
                }
            }

            return modified;
        }

        private static bool IsAspectType(TypeDefinition type)
        {
            return CecilExtensions.IsOrDerivesFrom(type, MetaNames.FieldSetAspect) ||
                   CecilExtensions.IsOrDerivesFrom(type, MetaNames.FieldGetAspect) ||
                   CecilExtensions.IsOrDerivesFrom(type, MetaNames.FieldChangeAspect) ||
                   CecilExtensions.IsOrDerivesFrom(type, MetaNames.MethodAspect);
        }

        private static IEnumerable<TypeDefinition> EnumerateTypes(IEnumerable<TypeDefinition> roots)
        {
            foreach (TypeDefinition type in roots)
            {
                yield return type;

                foreach (TypeDefinition nested in EnumerateTypes(type.NestedTypes))
                {
                    yield return nested;
                }
            }
        }
    }
}
