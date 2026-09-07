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
        int currentDepth)
    {
        if (currentDepth > maxDepth)
        {
            return null;
        }

        var node = new MemberNode(ToString(methodSyntax));

        foreach (var (subClassSyntax, subMethodSyntax) in GetInvokedMethods(classSyntax, methodSyntax))
        {
            var childNode = await AnalyzeMethod(subClassSyntax, subMethodSyntax, currentDepth + 1);
            if (childNode != null)
            {
                node.AddChild(childNode);
            }
        }

        return node;
    }

    private IEnumerable<(ClassDeclarationSyntax, MethodDeclarationSyntax)> GetInvokedMethods(
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
                    yield return handlerTypedSymbol;
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
                            TypeKind: TypeKind.Interface or TypeKind.Class,
                            MetadataToken: 0,
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
                        yield return (classSyntax, methodDeclarationSyntax);
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
                                continue;
                            }

                            var methodSymbol = methodSemanticModel.FindMethodImplementation(implementingClassSyntax, methodInvocation);

                            if (methodSymbol == null)
                            {
                                continue;
                            }

                            var invokedMethodSyntax = parsedSolution.FindMethodDeclaration(methodSymbol);

                            if (invokedMethodSyntax != null)
                            {
                                yield return invokedMethodSyntax.Value;
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

                            yield return invokedMethodSyntax;

                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException(nameof(fieldTypeSymbol.TypeKind), fieldTypeSymbol.TypeKind, "");
                    }
                }
            }
        }
    }

    private static string ToString(MethodDeclarationSyntax method)
    {
        var className = method.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault()?.Identifier.Text;
        var parameterTypes = method.ParameterList.Parameters.Where(x => x.Type != null).Select(x => x.Type!.ToFullString().Trim());

        return $"{className}.{method.Identifier.Text}({string.Join(",", parameterTypes)})";
    }

    private static string GetTabs(int count) => $"{Enumerable.Repeat("   ", count).JoinStrings(string.Empty)}\u2514\u2500\u2500";

    private static void WriteWithTab(int depth, string text) => Console.WriteLine($"{GetTabs(depth)}{text}");

    public static void WriteHierarchy(MemberNode node, int depth = 0)
    {
        ArgumentNullException.ThrowIfNull(node);
        WriteWithTab(depth, node.MethodSignature);

        if (node.Children.Count == 0)
        {
            WriteWithTab(depth + 1, "<Empty>");
            return;
        }

        foreach (var child in node.Children)
        {
            WriteHierarchy(child, depth + 1);
        }
    }
}
