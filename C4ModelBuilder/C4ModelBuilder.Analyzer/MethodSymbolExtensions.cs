using Microsoft.CodeAnalysis;

namespace C4ModelBuilder.Analyzer;

internal static class MethodSymbolExtensions
{
    public static bool IsImplement(this IMethodSymbol classMethod, IMethodSymbol interfaceMethod)
        => classMethod.Name == interfaceMethod.Name
            && classMethod.TypeParameters.Length == interfaceMethod.TypeParameters.Length
            && TypeComparer.ParametersAreEquivalent(classMethod.Parameters, interfaceMethod.Parameters)
            && TypeComparer.TypesAreEquivalent(classMethod.ReturnType, interfaceMethod.ReturnType);
}
