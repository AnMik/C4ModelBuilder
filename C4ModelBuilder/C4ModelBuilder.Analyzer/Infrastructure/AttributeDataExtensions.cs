using C4ModelBuilder.Attributes;
using Microsoft.CodeAnalysis;

namespace C4ModelBuilder.Analyzer.Infrastructure;

internal static class AttributeDataExtensions
{
    public static bool IsRootC4Component(this AttributeData? attributeData)
        => attributeData
                ?.NamedArguments
                .Any(argument => argument is { Key: nameof(C4ComponentAttribute.IsRoot), Value.Value: true })
            == true;

    public static string? GetC4ComponentDescription(this AttributeData? attributeData)
        => attributeData
            ?.NamedArguments
            .Where(argument => argument is { Key: nameof(C4ComponentAttribute.Description) })
            .Select(argument => argument.Value.Value?.ToString())
            .FirstOrDefault();
}
