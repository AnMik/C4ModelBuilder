using Microsoft.CodeAnalysis;

namespace C4ModelBuilder.Analyzer.Infrastructure;

internal static class AttributeDataExtensions
{
    public static bool IsRootC4Component(this AttributeData? attributeData)
        => attributeData is { ConstructorArguments: [_, { Value: true }, ..] };

    public static string? GetC4ComponentDescription(this AttributeData? attributeData)
        => attributeData is { ConstructorArguments: [{ Value: var value }, ..] }
            ? value?.ToString()
            : null;

    public static string? GetC4ComponentTechnology(this AttributeData? attributeData)
        => attributeData is { ConstructorArguments: [_, _, { Value: var value }, ..] }
            ? value?.ToString()
            : null;
}
