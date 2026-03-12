using Microsoft.CodeAnalysis;

namespace C4ModelBuilder.Analyzer;

public static class TypeSymbolExtensions
{
    private static bool TypesAreCompatible(ITypeSymbol classType, ITypeSymbol interfaceType)
    {
        if (classType.Equals(interfaceType, SymbolEqualityComparer.Default))
        {
            return true;
        }

        if (classType.IsImplement(interfaceType))
        {
            return true;
        }

        if (classType is INamedTypeSymbol classNamedType && interfaceType is INamedTypeSymbol interfaceNamedType)
        {
            return GenericTypesAreCompatible(classNamedType, interfaceNamedType);
        }

        return IsCollectionCompatible(classType, interfaceType);
    }

    private static bool IsImplement(this ITypeSymbol classSymbol, ITypeSymbol interfaceSymbol)
    {
        var currentClass = classSymbol;

        while (currentClass != null)
        {
            if (currentClass.Equals(interfaceSymbol, SymbolEqualityComparer.Default))
            {
                return true;
            }

            if (currentClass.AllInterfaces.Any(x => x.Equals(interfaceSymbol, SymbolEqualityComparer.Default)))
            {
                return true;
            }

            currentClass = currentClass.BaseType;
        }

        return false;
    }

    private static bool GenericTypesAreCompatible(INamedTypeSymbol classType, INamedTypeSymbol interfaceType)
    {
        if (!classType.OriginalDefinition.Equals(interfaceType.OriginalDefinition, SymbolEqualityComparer.Default))
        {
            return false;
        }

        if (classType.TypeArguments.Length != interfaceType.TypeArguments.Length)
        {
            return false;
        }

        for (var i = 0; i < classType.TypeArguments.Length; i++)
        {
            if (!TypesAreCompatible(classType.TypeArguments[i], interfaceType.TypeArguments[i]))
            {
                return false;
            }
        }
        return true;

    }

    private static bool IsCollectionCompatible(this ITypeSymbol classType, ITypeSymbol interfaceType)
    {
        // T[] → IEnumerable<T>
        if (classType is IArrayTypeSymbol arrayType &&
            interfaceType.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>")
        {
            var elementType = arrayType.ElementType;
            var interfaceElementType = ((INamedTypeSymbol)interfaceType).TypeArguments[0];
            return TypesAreCompatible(elementType, interfaceElementType);
        }

        // List<T> → IReadOnlyCollection<T>
        if (classType.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.List<T>" &&
            interfaceType.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IReadOnlyCollection<T>")
        {
            var classElementType = ((INamedTypeSymbol)classType).TypeArguments[0];
            var interfaceElementType = ((INamedTypeSymbol)interfaceType).TypeArguments[0];
            return TypesAreCompatible(classElementType, interfaceElementType);
        }

        return false;
    }
}
