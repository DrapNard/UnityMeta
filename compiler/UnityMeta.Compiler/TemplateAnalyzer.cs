using System.Collections.Generic;
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
            "Value-transform templates must return a value",
            "Template '{0}' must return the transformed field value",
            "UnityMeta",
            DiagnosticSeverity.Error,
            true);

        private static readonly DiagnosticDescriptor BoundaryReturn = new DiagnosticDescriptor(
            "UMETA003",
            "Observer templates must return void",
            "Template '{0}' must return void",
            "UnityMeta",
            DiagnosticSeverity.Error,
            true);

        private static readonly DiagnosticDescriptor MultipleTemplateKinds = new DiagnosticDescriptor(
            "UMETA004",
            "A template method must have one role",
            "Template method '{0}' has more than one template marker",
            "UnityMeta",
            DiagnosticSeverity.Error,
            true);

        private static readonly DiagnosticDescriptor ParameterBinding = new DiagnosticDescriptor(
            "UMETA005",
            "Template parameters need one binding",
            "Template parameter '{0}' on '{1}' must have exactly one UnityMeta binding attribute",
            "UnityMeta",
            DiagnosticSeverity.Error,
            true);

        private static readonly DiagnosticDescriptor InvalidBinding = new DiagnosticDescriptor(
            "UMETA006",
            "Binding is invalid for this template",
            "Binding '{0}' is not valid on a {1} template parameter",
            "UnityMeta",
            DiagnosticSeverity.Error,
            true);

        private static readonly DiagnosticDescriptor WrongAspectBase = new DiagnosticDescriptor(
            "UMETA007",
            "Template does not match its aspect base class",
            "Template '{0}' requires its containing aspect to derive from '{1}'",
            "UnityMeta",
            DiagnosticSeverity.Error,
            true);

        private static readonly DiagnosticDescriptor GenericTemplate = new DiagnosticDescriptor(
            "UMETA008",
            "Generic templates are not supported yet",
            "Template method '{0}' cannot be generic in the current UnityMeta backend",
            "UnityMeta",
            DiagnosticSeverity.Error,
            true);

        private static readonly DiagnosticDescriptor PublicTemplate = new DiagnosticDescriptor(
            "UMETA009",
            "Template methods must be public",
            "Template method '{0}' must be public while the current backend emits direct calls",
            "UnityMeta",
            DiagnosticSeverity.Error,
            true);

        private static readonly string[] BindingAttributes =
        {
            "UnityMeta.ValueAttribute",
            "UnityMeta.OldValueAttribute",
            "UnityMeta.NewValueAttribute",
            "UnityMeta.ReturnValueAttribute",
            "UnityMeta.AspectArgumentAttribute",
            "UnityMeta.AspectNamedArgumentAttribute",
            "UnityMeta.TargetMemberNameAttribute",
            "UnityMeta.TargetTypeNameAttribute",
            "UnityMeta.TargetInstanceAttribute",
            "UnityMeta.TargetArgumentAttribute",
            "UnityMeta.FieldValueFromAspectArgumentAttribute"
        };

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(
                    StaticTemplate,
                    SetReturn,
                    BoundaryReturn,
                    MultipleTemplateKinds,
                    ParameterBinding,
                    InvalidBinding,
                    WrongAspectBase,
                    GenericTemplate,
                    PublicTemplate);
            }
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
            bool get = HasAttribute(method, "UnityMeta.GetTemplateAttribute");
            bool change = HasAttribute(method, "UnityMeta.ChangeTemplateAttribute");
            bool before = HasAttribute(method, "UnityMeta.BeforeTemplateAttribute");
            bool after = HasAttribute(method, "UnityMeta.AfterTemplateAttribute");

            int kindCount = (set ? 1 : 0) + (get ? 1 : 0) + (change ? 1 : 0) + (before ? 1 : 0) + (after ? 1 : 0);
            if (kindCount == 0)
            {
                return;
            }

            Location location = method.Locations.Length > 0 ? method.Locations[0] : Location.None;

            if (kindCount > 1)
            {
                context.ReportDiagnostic(Diagnostic.Create(MultipleTemplateKinds, location, method.Name));
                return;
            }

            if (!method.IsStatic)
            {
                context.ReportDiagnostic(Diagnostic.Create(StaticTemplate, location, method.Name));
            }

            if (method.IsGenericMethod)
            {
                context.ReportDiagnostic(Diagnostic.Create(GenericTemplate, location, method.Name));
            }

            if (method.DeclaredAccessibility != Accessibility.Public)
            {
                context.ReportDiagnostic(Diagnostic.Create(PublicTemplate, location, method.Name));
            }

            if ((set || get) && method.ReturnsVoid)
            {
                context.ReportDiagnostic(Diagnostic.Create(SetReturn, location, method.Name));
            }

            if (!set && !get && !method.ReturnsVoid)
            {
                context.ReportDiagnostic(Diagnostic.Create(BoundaryReturn, location, method.Name));
            }

            string requiredBase = set
                ? "UnityMeta.FieldSetAspectAttribute"
                : get
                    ? "UnityMeta.FieldGetAspectAttribute"
                    : change
                    ? "UnityMeta.FieldChangeAspectAttribute"
                    : "UnityMeta.MethodAspectAttribute";

            if (!ContainingTypeDerivesFrom(method.ContainingType, requiredBase))
            {
                context.ReportDiagnostic(Diagnostic.Create(WrongAspectBase, location, method.Name, requiredBase));
            }

            string templateKind = set ? "set" : get ? "get" : change ? "change" : before ? "before" : "after";

            foreach (IParameterSymbol parameter in method.Parameters)
            {
                var bindings = GetBindings(parameter);
                Location parameterLocation = parameter.Locations.Length > 0 ? parameter.Locations[0] : location;

                if (bindings.Count != 1)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(ParameterBinding, parameterLocation, parameter.Name, method.Name));
                    continue;
                }

                string binding = bindings[0];
                if (!IsBindingAllowed(binding, set, get, change, before, after))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(InvalidBinding, parameterLocation, ShortName(binding), templateKind));
                }
            }
        }

        private static List<string> GetBindings(IParameterSymbol parameter)
        {
            var result = new List<string>();
            foreach (AttributeData attribute in parameter.GetAttributes())
            {
                string? name = attribute.AttributeClass?.ToDisplayString();
                if (name == null)
                {
                    continue;
                }

                foreach (string binding in BindingAttributes)
                {
                    if (name == binding)
                    {
                        result.Add(name);
                        break;
                    }
                }
            }

            return result;
        }

        private static bool IsBindingAllowed(
            string binding,
            bool set,
            bool get,
            bool change,
            bool before,
            bool after)
        {
            if (binding == "UnityMeta.ValueAttribute")
            {
                return set || get;
            }

            if (binding == "UnityMeta.OldValueAttribute" || binding == "UnityMeta.NewValueAttribute")
            {
                return change;
            }

            if (binding == "UnityMeta.ReturnValueAttribute")
            {
                return after;
            }

            if (binding == "UnityMeta.TargetArgumentAttribute")
            {
                return before || after;
            }

            if (binding == "UnityMeta.FieldValueFromAspectArgumentAttribute")
            {
                return set || get || change;
            }

            return true;
        }

        private static bool ContainingTypeDerivesFrom(INamedTypeSymbol type, string fullName)
        {
            INamedTypeSymbol? current = type;
            while (current != null)
            {
                if (current.ToDisplayString() == fullName)
                {
                    return true;
                }

                current = current.BaseType;
            }

            return false;
        }

        private static string ShortName(string fullName)
        {
            const string prefix = "UnityMeta.";
            string name = fullName.StartsWith(prefix) ? fullName.Substring(prefix.Length) : fullName;
            return name.EndsWith("Attribute") ? name.Substring(0, name.Length - "Attribute".Length) : name;
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
