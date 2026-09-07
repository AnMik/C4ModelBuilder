using Microsoft.CodeAnalysis;

namespace C4ModelBuilder.Analyzer.Infrastructure;

internal static class NamedTypeSymbolExtensions
{
    public static IMethodSymbol? FindImplementationOf(this INamedTypeSymbol implementingClass, IMethodSymbol interfaceMethod)
    {
        var baseType = implementingClass;

        while (baseType != null)
        {
            var member = baseType.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(member => member.IsImplement(interfaceMethod));

            if (member != null)
            {
                return member;
            }

            baseType = baseType.BaseType;
        }

        return null;
    }

    private static bool IsImplement(this IMethodSymbol classMethod, IMethodSymbol interfaceMethod)
        => classMethod.Name == interfaceMethod.Name
            && classMethod.TypeParameters.Length == interfaceMethod.TypeParameters.Length
            && TypeComparer.ParametersAreEquivalent(classMethod.Parameters, interfaceMethod.Parameters)
            && TypeComparer.TypesAreEquivalent(classMethod.ReturnType, interfaceMethod.ReturnType);
}
