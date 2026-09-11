using C4ModelBuilder.Analyzer.Infrastructure;
using Microsoft.CodeAnalysis;

namespace C4ModelBuilder.Analyzer.Models;

internal sealed class InvokedMethod
{
    public ClassMethod? ClassMethod { get; }

    public string ClassName { get; }

    public string MethodName { get; }

    /// <summary>
    /// Описание из атрибута <c>C4Component</c> (свойство <c>Description</c>) для типа/класса/интерфейса;
    /// <c>null</c>, если атрибут не проставлен или описание не задано.
    /// </summary>
    public string? ClassDescription { get; }

    /// <summary>
    /// Описание из атрибута <c>C4Component</c> (свойство <c>Description</c>) для метода;
    /// <c>null</c>, если атрибут не проставлен или описание не задано.
    /// </summary>
    public string? MethodDescription { get; }

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
        ClassDescription = classSymbol.GetC4ComponentAttribute().GetC4ComponentDescription();
        MethodDescription = methodSymbol?.GetC4ComponentAttribute().GetC4ComponentDescription();
    }

    public static InvokedMethod From(ClassMethod classMethod, INamedTypeSymbol classSymbol, IMethodSymbol? methodSymbol)
        => new(classMethod, className: null, methodName: null, classSymbol, methodSymbol);

    public static InvokedMethod From(INamedTypeSymbol classSymbol, IMethodSymbol? methodSymbol)
        => new(classMethod: null, classSymbol.Name, methodSymbol?.Name ?? "<not_found>", classSymbol, methodSymbol);
}
