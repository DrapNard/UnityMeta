using System.Collections.Generic;
using Unity.CompilationPipeline.Common.Diagnostics;
using UnityMeta.Weaver;

namespace UnityMeta.Editor.CodeGen
{
    internal sealed class UnityPostProcessorLogger : IMetaLogger
    {
        private readonly List<DiagnosticMessage> _messages = new List<DiagnosticMessage>();

        public List<DiagnosticMessage> Messages
        {
            get { return _messages; }
        }

        public void Warning(string message)
        {
            Add(message, DiagnosticType.Warning);
        }

        public void Error(string message)
        {
            Add(message, DiagnosticType.Error);
        }

        private void Add(string message, DiagnosticType type)
        {
            _messages.Add(new DiagnosticMessage
            {
                DiagnosticType = type,
                File = null,
                Line = 0,
                Column = 0,
                MessageData = message
            });
        }
    }
}
