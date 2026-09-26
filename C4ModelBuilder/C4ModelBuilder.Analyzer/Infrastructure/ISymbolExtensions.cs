using C4ModelBuilder.Attributes;
using Microsoft.CodeAnalysis;

namespace C4ModelBuilder.Analyzer.Infrastructure;

internal static class ISymbolExtensions
{
    public static AttributeData? GetC4ComponentAttribute(this ISymbol symbol)
        => symbol.GetAttributes().FirstOrDefault(attribute => attribute.AttributeClass?.Name == nameof(C4ComponentAttribute));
}
