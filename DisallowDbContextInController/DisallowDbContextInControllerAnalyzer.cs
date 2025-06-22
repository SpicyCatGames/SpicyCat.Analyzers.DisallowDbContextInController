using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace SpicyCat.Analyzers.DisallowDbContextInController
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DisallowDbContextInControllerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DisallowDbContextInController";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "DbContext should not be injected into controllers",
            "DbContext should not be injected into controller '{0}'",
            "Architecture",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeConstructor, SyntaxKind.ConstructorDeclaration);
        }

        private void AnalyzeConstructor(SyntaxNodeAnalysisContext context)
        {
            var constructor = (ConstructorDeclarationSyntax)context.Node;

            var classDecl = constructor.Parent as ClassDeclarationSyntax;
            if (classDecl == null) return;

            var semanticModel = context.SemanticModel;
            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);

            // Only apply this to Controllers
            if (!IsController(classSymbol)) return;

            foreach (var parameter in constructor.ParameterList.Parameters)
            {
                var typeInfo = semanticModel.GetTypeInfo(parameter.Type);
                var typeSymbol = typeInfo.Type;

                if (typeSymbol == null) continue;

                if (typeSymbol.Name.EndsWith("DbContext"))
                {
                    var diagnostic = Diagnostic.Create(Rule, parameter.GetLocation(), classSymbol.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private bool IsController(INamedTypeSymbol? classSymbol)
        {
            while (classSymbol != null)
            {
                if (classSymbol.ToString() == "Microsoft.AspNetCore.Mvc.ControllerBase")
                    return true;
                classSymbol = classSymbol.BaseType;
            }

            return false;
        }
    }
}
