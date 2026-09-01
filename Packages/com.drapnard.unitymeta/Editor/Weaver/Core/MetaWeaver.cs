using System;
using System.Collections.Generic;
using Mono.Cecil;

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
            var fieldWeaver = new FieldStoreWeaver(assembly.MainModule, _logger);
            var methodWeaver = new MethodAspectWeaver(assembly.MainModule, _logger);

            foreach (TypeDefinition type in EnumerateTypes(assembly.MainModule.Types))
            {
                // Do not weave template methods themselves. They are ordinary
                // runtime helper methods in v0.1 and can later be inlined by a
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

                    if (fieldWeaver.Process(method))
                    {
                        modified = true;
                    }

                    if (methodWeaver.Process(method))
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
