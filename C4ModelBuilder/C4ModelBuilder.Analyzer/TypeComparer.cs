using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace C4ModelBuilder.Analyzer;

internal static class TypeComparer
{
    // Формат для сравнения типов без информации о сборке
    private static readonly SymbolDisplayFormat TypeKeyFormat =
        new(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable);

    public static bool TypesAreEquivalent(ITypeSymbol? typeSymbol1, ITypeSymbol? typeSymbol2)
        => typeSymbol1 == null && typeSymbol2 == null
            || typeSymbol1 != null && typeSymbol2 != null && GetTypeKey(typeSymbol1) == GetTypeKey(typeSymbol2);

    private static string GetTypeKey(ITypeSymbol? typeSymbol) => typeSymbol?.ToDisplayString(TypeKeyFormat) ?? "null";

    public static bool ParametersAreEquivalent(ImmutableArray<IParameterSymbol> parameters1, ImmutableArray<IParameterSymbol> parameters2)
        => parameters1.Length == parameters2.Length
            && parameters1.Zip(parameters2, (x, y) => TypesAreEquivalent(x.Type, y.Type)).All(x => x);
}
