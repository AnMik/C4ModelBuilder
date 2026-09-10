using C4ModelBuilder.Models.Attributes;
using Microsoft.CodeAnalysis;

namespace C4ModelBuilder.Analyzer.Infrastructure;

internal static class C4ComponentAttributeExtensions
{
    public static AttributeData? GetC4ComponentAttribute(this ISymbol? symbol)
        => symbol
            ?.GetAttributes()
            .FirstOrDefault(attribute => attribute.AttributeClass?.Name == nameof(C4ComponentAttribute));

    public static bool HasC4ComponentAttribute(this ISymbol? symbol)
        => symbol.GetC4ComponentAttribute() != null;

    public static bool IsRootComponent(this ISymbol? symbol)
        => symbol
                .GetC4ComponentAttribute()
                ?.NamedArguments.Any(argument => argument is { Key: nameof(C4ComponentAttribute.IsRoot), Value.Value: true })
            == true;
}
