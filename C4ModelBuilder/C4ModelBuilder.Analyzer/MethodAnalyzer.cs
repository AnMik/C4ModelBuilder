using C4ModelBuilder.Analyzer.Infrastructure;
using C4ModelBuilder.Analyzer.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C4ModelBuilder.Analyzer;

internal sealed class MethodAnalyzer(ParsedSolution parsedSolution, Dictionary<string, ClassMethod> cqrsHandlersMapping, int maxDepth)
{
    public async Task<MemberNode?> AnalyzeMethod(
        ClassMethod classMethod,
        int currentDepth,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (currentDepth > maxDepth)
        {
            return null;
        }

        var node = new MemberNode(classMethod.Name);

        foreach (var invoked in GetInvokedMethods(classMethod))
        {
            var typeNode = new MemberNode(invoked.ClassName);

            var methodChild = invoked.ClassMethod != null
                ? await AnalyzeMethod(invoked.ClassMethod, currentDepth + 1, ct)
                : new MemberNode($"{invoked.ClassName}.{invoked.MethodName}");

            if (methodChild != null)
            {
                typeNode.AddChild(methodChild);
                node.AddChild(typeNode);
            }
        }

        return node;
    }

    private IEnumerable<InvokedMethod> GetInvokedMethods(ClassMethod classMethod)
    {
        var methodSemanticModel =
            parsedSolution
                .Projects
                .Where(x => x.Compilation.SyntaxTrees.Contains(classMethod.MethodSyntax.SyntaxTree))
                .Select(x => x.Compilation.GetSemanticModel(classMethod.MethodSyntax.SyntaxTree))
                .FirstOrDefault()
            ?? throw new InvalidOperationException($"Не найдена семантическая модель для метода {classMethod.MethodSyntax}");

        foreach (var methodInvocation in classMethod.MethodSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var nameSyntaxesOfMethodInvocations =
                methodInvocation
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
                var invocationArguments =
                    methodInvocation
                        .ChildNodes()
                        .OfType<ArgumentListSyntax>()
                        .SelectMany(x => x.ChildNodes())
                        .OfType<ArgumentSyntax>()
                        .SelectMany(x => x.ChildNodes())
                        .ToList();

                var baseRequest =
                    invocationArguments
                        .OfType<ObjectCreationExpressionSyntax>()
                        .SelectMany(x => x.ChildNodes())
                        .OfType<IdentifierNameSyntax>()
                        .Cast<SimpleNameSyntax>();

                var genericRequest =
                    invocationArguments
                        .OfType<InvocationExpressionSyntax>()
                        .SelectMany(x => x.ChildNodes())
                        .OfType<MemberAccessExpressionSyntax>()
                        .SelectMany(x => x.ChildNodes())
                        .OfType<ObjectCreationExpressionSyntax>()
                        .SelectMany(x => x.ChildNodes())
                        .OfType<GenericNameSyntax>()
                        .Cast<SimpleNameSyntax>();

                var genericRequestWithReadPreference =
                    invocationArguments
                        .OfType<ObjectCreationExpressionSyntax>()
                        .SelectMany(x => x.ChildNodes())
                        .OfType<GenericNameSyntax>()
                        .Cast<SimpleNameSyntax>();

                var queryName =
                    baseRequest
                        .Concat(genericRequest)
                        .Concat(genericRequestWithReadPreference)
                        .FirstOrDefault()
                        ?.Identifier.Text
                    ?? string.Empty;

                var handlerTypedSymbol = cqrsHandlersMapping.GetValueOrDefault(queryName)
                    ?? throw new InvalidOperationException($"Не найден cqrs хендлер для {queryName}.");

                yield return InvokedMethod.From(handlerTypedSymbol);
            }
            else
            {
                var parentClassFields =
                    classMethod
                        .ClassSyntax
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

                var fieldName =
                    methodInvocation
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

                    var methodDeclarationSyntax =
                        classMethod
                            .ClassSyntax
                            .Members
                            .OfType<MethodDeclarationSyntax>()
                            .FirstOrDefault(x => x.Identifier.Text == methodInvocationSyntaxName);

                    if (methodDeclarationSyntax != null)
                    {
                        yield return InvokedMethod.From(classMethod with { MethodSyntax = methodDeclarationSyntax });
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
                            var implementingClassSyntax =
                                parsedSolution
                                    .Projects
                                    .SelectMany(x => x.Classes)
                                    .Select(x => x.SemanticModel.GetDeclaredSymbol(x.ClassDeclarationSyntax))
                                    .FirstOrDefault(
                                        @class => @class?.AllInterfaces.Any(x => x.ToDisplayString() == fieldTypeSymbol.ToDisplayString())
                                            == true);

                            if (implementingClassSyntax == null)
                            {
                                // Имплементация интерфейса не найдена в солюшене (например, внешняя библиотека): добавляем узел-интерфейс и его метод как лист.
                                var externalMethodName =
                                    (methodSemanticModel.GetSymbolInfo(methodInvocation).Symbol as IMethodSymbol)?.Name
                                    ?? string.Empty;

                                yield return InvokedMethod.From(fieldTypeSymbol.Name, externalMethodName);

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
                                yield return InvokedMethod.From(invokedMethodSyntax);
                            }

                            break;
                        }
                        case TypeKind.Class:
                        {
                            var methodSymbol = methodSemanticModel.FindMethodImplementation(fieldTypeSymbol, methodInvocation)
                                ?? throw new InvalidOperationException(
                                    $"Не найден метод {methodSemanticModel} в классе {fieldTypeSymbol.ToDisplayString()}.");

                            var invokingClassMethod = parsedSolution.FindMethodDeclaration(methodSymbol)
                                ?? throw new InvalidOperationException(
                                    $"Не найден метод {methodSemanticModel} в классе {fieldTypeSymbol.ToDisplayString()}.");

                            yield return InvokedMethod.From(invokingClassMethod);

                            break;
                        }
                        case TypeKind.Unknown:
                        case TypeKind.Array:
                        case TypeKind.Delegate:
                        case TypeKind.Dynamic:
                        case TypeKind.Enum:
                        case TypeKind.Error:
                        case TypeKind.Module:
                        case TypeKind.Pointer:
                        case TypeKind.Struct:
                        case TypeKind.TypeParameter:
                        case TypeKind.Submission:
                        case TypeKind.FunctionPointer:
                        case TypeKind.Extension:
                        default:
                            throw new ArgumentOutOfRangeException(nameof(fieldTypeSymbol.TypeKind), fieldTypeSymbol.TypeKind, "");
                    }
                }
            }
        }
    }
}
