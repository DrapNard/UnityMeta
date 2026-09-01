using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UnityMeta.Compiler
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TemplateAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor StaticTemplate = new DiagnosticDescriptor(
            "UMETA001",
            "Template methods must be static",
            "Template method '{0}' must be static",
            "UnityMeta",
            DiagnosticSeverity.Error,
            true);

        private static readonly DiagnosticDescriptor SetReturn = new DiagnosticDescriptor(
            "UMETA002",
            "Set templates must return a value",
            "Set template '{0}' must return the transformed field value",
            "UnityMeta",
            DiagnosticSeverity.Error,
            true);

        private static readonly DiagnosticDescriptor BoundaryReturn = new DiagnosticDescriptor(
            "UMETA003",
            "Method boundary templates must return void",
            "Method boundary template '{0}' must return void",
            "UnityMeta",
            DiagnosticSeverity.Error,
            true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(StaticTemplate, SetReturn, BoundaryReturn); }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        }

        private static void AnalyzeMethod(SymbolAnalysisContext context)
        {
            var method = (IMethodSymbol)context.Symbol;
            bool set = HasAttribute(method, "UnityMeta.SetTemplateAttribute");
            bool before = HasAttribute(method, "UnityMeta.BeforeTemplateAttribute");
            bool after = HasAttribute(method, "UnityMeta.AfterTemplateAttribute");

            if (!set && !before && !after)
            {
                return;
            }

            Location location = method.Locations.Length > 0 ? method.Locations[0] : Location.None;

            if (!method.IsStatic)
            {
                context.ReportDiagnostic(Diagnostic.Create(StaticTemplate, location, method.Name));
            }

            if (set && method.ReturnsVoid)
            {
                context.ReportDiagnostic(Diagnostic.Create(SetReturn, location, method.Name));
            }

            if ((before || after) && !method.ReturnsVoid)
            {
                context.ReportDiagnostic(Diagnostic.Create(BoundaryReturn, location, method.Name));
            }
        }

        private static bool HasAttribute(IMethodSymbol method, string fullName)
        {
            foreach (AttributeData attribute in method.GetAttributes())
            {
                if (attribute.AttributeClass != null && attribute.AttributeClass.ToDisplayString() == fullName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
