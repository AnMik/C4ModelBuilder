using C4ModelBuilder.Analyzer.Infrastructure;
using Microsoft.CodeAnalysis;

namespace C4ModelBuilder.Analyzer.Models;

internal sealed class InvokedMethod
{
    public ClassMethod? ClassMethod { get; }

    public string ClassName { get; }

    public string MethodName { get; }

    /// <summary>
    /// Признак того, что на типе/классе/интерфейсе проставлен атрибут <c>C4Component</c>.
    /// </summary>
    public bool IsClassComponent { get; }

    /// <summary>
    /// Признак того, что на методе проставлен атрибут <c>C4Component</c>.
    /// </summary>
    public bool IsMethodComponent { get; }

    private InvokedMethod(
        ClassMethod? classMethod,
        string? className,
        string? methodName,
        INamedTypeSymbol classSymbol,
        IMethodSymbol? methodSymbol)
    {
        ClassMethod = classMethod;
        ClassName = classMethod?.ClassSyntax.Identifier.Text ?? className ?? throw new ArgumentNullException(nameof(className));
        MethodName = classMethod?.MethodSyntax.Identifier.Text ?? methodName ?? throw new ArgumentNullException(nameof(methodName));
        IsClassComponent = classSymbol.HasC4ComponentAttribute();
        IsMethodComponent = methodSymbol.HasC4ComponentAttribute();
    }

    public static InvokedMethod From(ClassMethod classMethod, INamedTypeSymbol classSymbol, IMethodSymbol? methodSymbol)
        => new(classMethod, className: null, methodName: null, classSymbol, methodSymbol);

    public static InvokedMethod From(INamedTypeSymbol classSymbol, IMethodSymbol? methodSymbol)
        => new(classMethod: null, classSymbol.Name, methodSymbol?.Name ?? "<not_found>", classSymbol, methodSymbol);
}
