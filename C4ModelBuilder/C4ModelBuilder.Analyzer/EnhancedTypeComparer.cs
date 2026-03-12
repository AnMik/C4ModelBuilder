using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace C4ModelBuilder.Analyzer;

public static class EnhancedTypeComparer
{
    // Проверка совместимости типов с учетом наследования
    public static bool TypesAreCompatible(ITypeSymbol sourceType, ITypeSymbol targetType)
    {
        // Если типы эквивалентны (без учета сборки)
        if (TypeComparer.TypesAreEquivalent(sourceType, targetType))
            return true;

        // Проверяем, является ли sourceType подтипом targetType
        if (IsSubtypeOf(sourceType, targetType))
            return true;

        // Проверяем совместимость generic-типов
        if (sourceType is INamedTypeSymbol sourceNamedType && targetType is INamedTypeSymbol targetNamedType)
        {
            return GenericTypesAreCompatible(sourceNamedType, targetNamedType);
        }

        return false;
    }

    private static bool IsSubtypeOf(ITypeSymbol type, ITypeSymbol potentialBaseType)
    {
        var current = type;
        while (current != null)
        {
            if (TypeComparer.TypesAreEquivalent(current, potentialBaseType))
            {
                return true;
            }

            // Проверяем интерфейсы
            if (current.AllInterfaces.Any(i => TypeComparer.TypesAreEquivalent(i, potentialBaseType)))
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    private static bool GenericTypesAreCompatible(INamedTypeSymbol sourceType, INamedTypeSymbol targetType)
    {
        // Проверяем, что оба типа являются generic-типами с одинаковым определением
        if (!TypeComparer.TypesAreEquivalent(sourceType.OriginalDefinition, targetType.OriginalDefinition))
        {
            return false;
        }

        if (sourceType.TypeArguments.Length != targetType.TypeArguments.Length)
        {
            return false;
        }

        // Проверяем совместимость generic-аргументов
        for (var i = 0; i < sourceType.TypeArguments.Length; i++)
        {
            if (!TypesAreCompatible(sourceType.TypeArguments[i], targetType.TypeArguments[i]))
            {
                return false;
            }
        }

        return true;
    }

    // Проверка совместимости параметров с учетом контравариантности
    public static bool ParametersAreCompatible(
        ImmutableArray<IParameterSymbol> sourceParameters,
        ImmutableArray<IParameterSymbol> targetParameters)
    {
        if (sourceParameters.Length != targetParameters.Length)
            return false;

        for (var i = 0; i < sourceParameters.Length; i++)
        {
            // Для параметров: targetType должен быть подтипом sourceType (контравариантность)
            if (!TypesAreCompatible(targetParameters[i].Type, sourceParameters[i].Type))
                return false;
        }

        return true;
    }
}
