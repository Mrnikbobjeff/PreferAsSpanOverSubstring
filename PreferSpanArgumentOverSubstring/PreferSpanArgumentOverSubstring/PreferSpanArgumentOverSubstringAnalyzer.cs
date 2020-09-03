using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PreferSpanArgumentOverSubstring
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PreferSpanArgumentOverSubstringAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "PreferSpanArgumentOverSubstring";
        static string[] SpanOrReadonlySpan = new[] { "Span", "ReadOnlySpan" };
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Performance";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.Argument);
        }

        static int NonDefaultCount<T>(T t) where T : IEnumerable<IParameterSymbol>
        {
            return t.Sum(x => x.HasExplicitDefaultValue ? 0 : 1);
        }

        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            var argument = (ArgumentSyntax)context.Node;

            if (!(argument.Expression is InvocationExpressionSyntax invocation && invocation.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Name.Identifier.ValueText.Equals("Substring")))
                return;
            var isInInvocation = argument.FirstAncestorOrSelf<InvocationExpressionSyntax>();
            if (isInInvocation is null)
                return;

            var isInNonAsyncMethod = argument.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (isInNonAsyncMethod is null)
                return;
            if (isInNonAsyncMethod.Modifiers.Any(SyntaxKind.AsyncKeyword))
                return;

            var currentInvocationMethod = context.SemanticModel.GetSymbolInfo(isInInvocation).Symbol as IMethodSymbol;
            if (currentInvocationMethod is null)
                return;

            var indexOfArg = isInInvocation.ArgumentList.Arguments.IndexOf(argument);
            if (currentInvocationMethod.Parameters[indexOfArg].Type.SpecialType != SpecialType.System_String)
                return;
            var methods = context.SemanticModel.GetMemberGroup(isInInvocation.Expression);


            if (methods.OfType<IMethodSymbol>().Where(x => NonDefaultCount(x.Parameters) == NonDefaultCount(currentInvocationMethod.Parameters) && currentInvocationMethod.Arity == x.Arity)
                .Where(x => SpanOrReadonlySpan.Contains(x.Parameters[indexOfArg].Type.Name)).Any())
            {
                //Span overload exists with same arity and parameter count with non defaults
                var diagnostic = Diagnostic.Create(Rule, argument.GetLocation(), argument);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
