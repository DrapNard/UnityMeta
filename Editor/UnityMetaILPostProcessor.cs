using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using UnityMeta.Weaver;

namespace UnityMeta.Editor.CodeGen
{
    /// <summary>
    /// Unity 2022 IL post-processor entry point.
    /// </summary>
    public sealed class UnityMetaILPostProcessor : ILPostProcessor
    {
        private const string RuntimeAssemblyName = "UnityMeta.Runtime";
        private const string IgnoreDefine = "UNITYMETA_DISABLE_WEAVING";

        public override ILPostProcessor GetInstance()
        {
            return this;
        }

        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            if (compiledAssembly.Defines != null && compiledAssembly.Defines.Contains(IgnoreDefine))
            {
                return false;
            }

            if (compiledAssembly.Name == RuntimeAssemblyName ||
                compiledAssembly.Name == "Unity.DrapNard.UnityMeta.CodeGen")
            {
                return false;
            }

            return compiledAssembly.References.Any(
                reference => Path.GetFileNameWithoutExtension(reference) == RuntimeAssemblyName);
        }

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            var logger = new UnityPostProcessorLogger();

            try
            {
                byte[] pdbData = compiledAssembly.InMemoryAssembly.PdbData ?? Array.Empty<byte>();

                using (var resolver = new UnityAssemblyResolver(compiledAssembly))
                using (var peInput = new MemoryStream(compiledAssembly.InMemoryAssembly.PeData))
                using (var pdbInput = new MemoryStream(pdbData))
                {
                    bool readSymbols = pdbData.Length > 0;
                    var reader = new ReaderParameters
                    {
                        AssemblyResolver = resolver,
                        ReadSymbols = readSymbols,
                        SymbolStream = readSymbols ? pdbInput : null
                    };

                    using (AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(peInput, reader))
                    {
                        var weaver = new MetaWeaver(logger);
                        bool modified = weaver.Weave(assembly);

                        if (!modified)
                        {
                            return new ILPostProcessResult(compiledAssembly.InMemoryAssembly, logger.Messages);
                        }

                        using (var peOutput = new MemoryStream())
                        using (var pdbOutput = new MemoryStream())
                        {
                            var writer = new WriterParameters
                            {
                                WriteSymbols = reader.ReadSymbols,
                                SymbolStream = reader.ReadSymbols ? pdbOutput : null,
                                SymbolWriterProvider = reader.ReadSymbols ? new PortablePdbWriterProvider() : null
                            };

                            assembly.Write(peOutput, writer);

                            byte[] pdb = reader.ReadSymbols ? pdbOutput.ToArray() : Array.Empty<byte>();
                            var result = new InMemoryAssembly(peOutput.ToArray(), pdb);
                            return new ILPostProcessResult(result, logger.Messages);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                logger.Error("UnityMeta weaving failed for '" + compiledAssembly.Name + "': " + exception);
                return new ILPostProcessResult(compiledAssembly.InMemoryAssembly, logger.Messages);
            }
        }
    }
}
