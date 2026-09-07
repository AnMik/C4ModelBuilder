using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C4ModelBuilder.Analyzer.Infrastructure;

internal static class SemanticModelExtensions
{
    public static IMethodSymbol? FindMethodImplementation(
        this SemanticModel methodSemanticModel,
        INamedTypeSymbol implementingClass,
        InvocationExpressionSyntax invocation)
    {
        if (methodSemanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol invokedMethodSymbol)
        {
            return implementingClass.FindImplementationOf(invokedMethodSymbol);
        }

        return null;
    }
}
