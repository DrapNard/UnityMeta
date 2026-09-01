using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace UnityMeta.Editor.CodeGen
{
    internal sealed class UnityAssemblyResolver : DefaultAssemblyResolver
    {
        private readonly HashSet<string> _directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public UnityAssemblyResolver(ICompiledAssembly assembly)
        {
            foreach (string reference in assembly.References)
            {
                string directory = Path.GetDirectoryName(reference);
                if (!string.IsNullOrEmpty(directory) && _directories.Add(directory))
                {
                    AddSearchDirectory(directory);
                }
            }
        }
    }
}
