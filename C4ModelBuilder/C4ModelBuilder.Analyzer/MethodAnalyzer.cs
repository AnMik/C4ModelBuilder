using C4ModelBuilder.Analyzer.Infrastructure;
using C4ModelBuilder.Analyzer.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C4ModelBuilder.Analyzer;

internal sealed class MethodAnalyzer(
    ParsedSolution parsedSolution,
    Dictionary<string, (ClassDeclarationSyntax, MethodDeclarationSyntax)> cqrsHandlersMapping,
    int maxDepth)
{
    public async Task<MemberNode?> AnalyzeMethod(
        ClassDeclarationSyntax classSyntax,
        MethodDeclarationSyntax methodSyntax,
        int currentDepth,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (currentDepth > maxDepth)
        {
            return null;
        }

        var node = new MemberNode(MethodName(classSyntax, methodSyntax));

        foreach (var invoked in GetInvokedMethods(classSyntax, methodSyntax))
        {
            var typeNode = new MemberNode(invoked.TypeName);

            var methodChild = invoked is { ClassSyntax: not null, MethodSyntax: not null }
                ? await AnalyzeMethod(invoked.ClassSyntax, invoked.MethodSyntax, currentDepth + 1, ct)
                : new MemberNode($"{invoked.TypeName}.{invoked.MethodName}");

            if (methodChild != null)
            {
                typeNode.AddChild(methodChild);
                node.AddChild(typeNode);
            }
        }

        return node;
    }

    /// <summary>
    /// Разрешённый вызов: внутренний метод класса (ClassSyntax/MethodSyntax заданы, можно рекурсивно
    /// анализировать тело) либо внешний метод интерфейса без имплементации (MethodSyntax == null, лист).
    /// </summary>
    private sealed record InvokedMethod(
        string TypeName,
        ClassDeclarationSyntax? ClassSyntax,
        MethodDeclarationSyntax? MethodSyntax,
        string MethodName);

    private static string MethodName(ClassDeclarationSyntax classSyntax, MethodDeclarationSyntax methodSyntax)
        => $"{classSyntax.Identifier.Text}.{methodSyntax.Identifier.Text}";

    private static InvokedMethod FromSyntax(ClassDeclarationSyntax classSyntax, MethodDeclarationSyntax methodSyntax)
        => new(classSyntax.Identifier.Text, classSyntax, methodSyntax, methodSyntax.Identifier.Text);

    private IEnumerable<InvokedMethod> GetInvokedMethods(
        ClassDeclarationSyntax classSyntax,
        MethodDeclarationSyntax methodSyntax)
    {
        var methodSemanticModel = parsedSolution
                .Projects
                .Where(x => x.Compilation.SyntaxTrees.Contains(methodSyntax.SyntaxTree))
                .Select(x => x.Compilation.GetSemanticModel(methodSyntax.SyntaxTree))
                .FirstOrDefault()
            ?? throw new InvalidOperationException($"Не найдена семантическая модель для метода {methodSyntax}");

        foreach (var methodInvocation in methodSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var nameSyntaxesOfMethodInvocations = methodInvocation
                .ChildNodes()
                .OfType<MemberAccessExpressionSyntax>()
                .SelectMany(x => x.ChildNodes())
                .OfType<IdentifierNameSyntax>()
                .FirstOrDefault();

            var fieldTypeFullName = nameSyntaxesOfMethodInvocations != null
                ? methodSemanticModel.GetTypeInfo(nameSyntaxesOfMethodInvocations).Type?.ToDisplayString()
                : null;

            if (fieldTypeFullName is "Rds.Cqrs.Queries.IQueryService" or "Rds.Cqrs.Commands.ICommandProcessor")
            {
                var invocationArguments = methodInvocation
                    .ChildNodes()
                    .OfType<ArgumentListSyntax>()
                    .SelectMany(x => x.ChildNodes())
                    .OfType<ArgumentSyntax>()
                    .SelectMany(x => x.ChildNodes())
                    .ToList();

                var baseRequest = invocationArguments
                    .OfType<ObjectCreationExpressionSyntax>()
                    .SelectMany(x => x.ChildNodes())
                    .OfType<IdentifierNameSyntax>()
                    .Cast<SimpleNameSyntax>();

                var genericRequest = invocationArguments
                    .OfType<InvocationExpressionSyntax>()
                    .SelectMany(x => x.ChildNodes())
                    .OfType<MemberAccessExpressionSyntax>()
                    .SelectMany(x => x.ChildNodes())
                    .OfType<ObjectCreationExpressionSyntax>()
                    .SelectMany(x => x.ChildNodes())
                    .OfType<GenericNameSyntax>()
                    .Cast<SimpleNameSyntax>();

                var genericRequestWithReadPreference = invocationArguments
                    .OfType<ObjectCreationExpressionSyntax>()
                    .SelectMany(x => x.ChildNodes())
                    .OfType<GenericNameSyntax>()
                    .Cast<SimpleNameSyntax>();

                var queryName = baseRequest
                        .Concat(genericRequest)
                        .Concat(genericRequestWithReadPreference)
                        .FirstOrDefault()
                        ?.Identifier.Text
                    ?? string.Empty;

                if (cqrsHandlersMapping.TryGetValue(queryName, out var handlerTypedSymbol))
                {
                    yield return FromSyntax(handlerTypedSymbol.Item1, handlerTypedSymbol.Item2);
                }
                else
                {
                    throw new InvalidOperationException($"Не найден cqrs хендлер для {queryName}.");
                }
            }
            else
            {
                var parentClassFields = classSyntax
                    .Members
                    .OfType<FieldDeclarationSyntax>()
                    .SelectMany(x => x.Declaration.Variables)
                    .Select(
                        x => (FieldName: x.Identifier.Text,
                              FieldType: (methodSemanticModel.GetDeclaredSymbol(x) as IFieldSymbol)?.Type as INamedTypeSymbol))
                    .Where(
                        x => x.FieldType is
                            {
                                TypeKind: TypeKind.Class,
                                MetadataToken: 0,
                                Name: not "IMapper" and not "ITaggableCache"
                            }
                            or
                            {
                                TypeKind: TypeKind.Interface,
                                Name: not "IMapper" and not "ITaggableCache"
                            })
                    .ToDictionary(x => x.FieldName, x => x.FieldType!);

                var fieldName = methodInvocation
                    .ChildNodes()
                    .OfType<MemberAccessExpressionSyntax>()
                    .Select(x => x.Expression)
                    .OfType<IdentifierNameSyntax>()
                    .FirstOrDefault()
                    ?.Identifier.Text;

                if (fieldName == null)
                {
                    var methodInvocationSyntaxName =
                        methodInvocation
                            .ChildNodes()
                            .OfType<IdentifierNameSyntax>()
                            .FirstOrDefault()
                            ?.Identifier.Text;

                    var methodDeclarationSyntax = classSyntax
                        .Members
                        .OfType<MethodDeclarationSyntax>()
                        .FirstOrDefault(x => x.Identifier.Text == methodInvocationSyntaxName);

                    if (methodDeclarationSyntax != null)
                    {
                        yield return FromSyntax(classSyntax, methodDeclarationSyntax);
                    }
                }
                else
                {
                    if (!parentClassFields.TryGetValue(fieldName, out var fieldTypeSymbol))
                    {
                        continue;
                    }

                    switch (fieldTypeSymbol.TypeKind)
                    {
                        case TypeKind.Interface:
                        {
                            var implementingClassSyntax = parsedSolution
                                .Projects
                                .SelectMany(x => x.Classes)
                                .Select(x => x.SemanticModel.GetDeclaredSymbol(x.ClassDeclarationSyntax))
                                .FirstOrDefault(
                                    @class => @class?.AllInterfaces.Any(x => x.ToDisplayString() == fieldTypeSymbol.ToDisplayString())
                                        == true);

                            if (implementingClassSyntax == null)
                            {
                                // Имплементация интерфейса не найдена в солюшене (например, внешняя библиотека):
                                // добавляем узел-интерфейс и его метод как лист.
                                var externalMethodName =
                                    (methodSemanticModel.GetSymbolInfo(methodInvocation).Symbol as IMethodSymbol)?.Name
                                    ?? string.Empty;

                                yield return new InvokedMethod(fieldTypeSymbol.Name, null, null, externalMethodName);

                                break;
                            }

                            var methodSymbol = methodSemanticModel.FindMethodImplementation(implementingClassSyntax, methodInvocation);

                            if (methodSymbol == null)
                            {
                                continue;
                            }

                            var invokedMethodSyntax = parsedSolution.FindMethodDeclaration(methodSymbol);

                            if (invokedMethodSyntax != null)
                            {
                                yield return FromSyntax(invokedMethodSyntax.Value.Item1, invokedMethodSyntax.Value.Item2);
                            }

                            break;
                        }
                        case TypeKind.Class:
                        {
                            var methodSymbol = methodSemanticModel.FindMethodImplementation(fieldTypeSymbol, methodInvocation)
                                ?? throw new InvalidOperationException(
                                    $"Не найден метод {methodSemanticModel} в классе {fieldTypeSymbol.ToDisplayString()}.");

                            var invokedMethodSyntax = parsedSolution.FindMethodDeclaration(methodSymbol)
                                ?? throw new InvalidOperationException(
                                    $"Не найден метод {methodSemanticModel} в классе {fieldTypeSymbol.ToDisplayString()}.");

                            yield return FromSyntax(invokedMethodSyntax.Item1, invokedMethodSyntax.Item2);

                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException(nameof(fieldTypeSymbol.TypeKind), fieldTypeSymbol.TypeKind, "");
                    }
                }
            }
        }
    }
}
