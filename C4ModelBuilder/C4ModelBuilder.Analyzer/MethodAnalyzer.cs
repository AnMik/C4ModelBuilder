using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C4ModelBuilder.Analyzer;

internal sealed class MethodAnalyzer(
    IReadOnlyCollection<ParsedProject> projects,
    Dictionary<string, (ClassDeclarationSyntax, MethodDeclarationSyntax)> cqrsHandlersMapping)
{
    public async Task AnalyzeMethod(ClassDeclarationSyntax classSyntax, MethodDeclarationSyntax methodSyntax, int depth)
    {
        if (depth > 15)
        {
            WriteWithTab(depth + 1, "<Limit>");
            return;
        }

        WriteWithTab(depth, ToString(methodSyntax));

        var isEmpty = true;
        foreach (var (subClassSyntax, subMethodSyntax) in GetInvokedMethods(classSyntax, methodSyntax))
        {
            await AnalyzeMethod(subClassSyntax, subMethodSyntax, depth + 1);
            isEmpty = false;
        }

        if (isEmpty)
        {
            WriteWithTab(depth + 1, "<Empty>");
        }
    }

    private IEnumerable<(ClassDeclarationSyntax, MethodDeclarationSyntax)> GetInvokedMethods(
        ClassDeclarationSyntax classSyntax,
        MethodDeclarationSyntax methodSyntax)
    {
        var methodSemanticModel = projects
                .Where(x => x.Compilation.SyntaxTrees.Contains(methodSyntax.SyntaxTree))
                .Select(x => x.Compilation.GetSemanticModel(methodSyntax.SyntaxTree))
                .FirstOrDefault()
            ?? throw new InvalidOperationException($"Не найдена семантическая модель для метода {methodSyntax}");

        var parentClassFields = classSyntax
            .Members
            .OfType<FieldDeclarationSyntax>()
            .SelectMany(x => x.Declaration.Variables)
            .Select(
                x => (FieldName: x.Identifier.Text,
                      FieldType: (methodSemanticModel.GetDeclaredSymbol(x) as IFieldSymbol)?.Type as INamedTypeSymbol))
            .Where(
                x => x.FieldType is
                    { TypeKind: TypeKind.Interface or TypeKind.Class, MetadataToken: 0, Name: not "IMapper" and not "ITaggableCache" })
            .ToDictionary(x => x.FieldName, x => x.FieldType!);

        foreach (var methodInvocations in methodSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var invocationChildNodes = methodInvocations.ChildNodes().ToList();

            var identifierNameSyntax = invocationChildNodes
                .OfType<MemberAccessExpressionSyntax>()
                .SelectMany(x => x.ChildNodes())
                .OfType<IdentifierNameSyntax>()
                .FirstOrDefault();

            var fieldTypeFullName = identifierNameSyntax != null
                ? methodSemanticModel.GetTypeInfo(identifierNameSyntax).Type?.ToDisplayString()
                : null;

            if (fieldTypeFullName is "Rds.Cqrs.Queries.IQueryService" or "Rds.Cqrs.Commands.ICommandProcessor")
            {
                var t1 = invocationChildNodes
                    .OfType<ArgumentListSyntax>()
                    .SelectMany(x => x.ChildNodes())
                    .OfType<ArgumentSyntax>()
                    .SelectMany(x => x.ChildNodes())
                    .OfType<ObjectCreationExpressionSyntax>()
                    .SelectMany(x => x.ChildNodes())
                    .OfType<IdentifierNameSyntax>()
                    .FirstOrDefault()
                    ?.Identifier
                    .Text;

                var t2 = invocationChildNodes
                    .OfType<ArgumentListSyntax>()
                    .SelectMany(x => x.ChildNodes())
                    .OfType<ArgumentSyntax>()
                    .SelectMany(x => x.ChildNodes())
                    .OfType<InvocationExpressionSyntax>()
                    .SelectMany(x => x.ChildNodes())
                    .OfType<MemberAccessExpressionSyntax>()
                    .SelectMany(x => x.ChildNodes())
                    .OfType<ObjectCreationExpressionSyntax>()
                    .SelectMany(x => x.ChildNodes())
                    .OfType<GenericNameSyntax>()
                    .FirstOrDefault()
                    ?.Identifier.Text; // todo: GetObjectsWithLinks<Widget>

                var t3 = invocationChildNodes
                    .OfType<ArgumentListSyntax>()
                    .SelectMany(x => x.ChildNodes())
                    .OfType<ArgumentSyntax>()
                    .SelectMany(x => x.ChildNodes())
                    .OfType<ObjectCreationExpressionSyntax>()
                    .SelectMany(x => x.ChildNodes())
                    .OfType<GenericNameSyntax>()
                    .FirstOrDefault()
                    ?.Identifier.Text; // invocationsyntax (.withreadpreference)

                var queryName = t1 ?? t2 ?? t3;

                // todo: without new (var query)

                var handlerTypedSymbol = cqrsHandlersMapping.GetValueOrDefault(queryName ?? string.Empty);
                if (handlerTypedSymbol == default)
                {
                    Console.WriteLine($"Не найден обработчик {queryName}.");
                    continue;
                }

                yield return handlerTypedSymbol;
            }
            else
            {
                var fieldName =
                    (invocationChildNodes.OfType<MemberAccessExpressionSyntax>().FirstOrDefault()?.Expression as IdentifierNameSyntax)
                    ?.Identifier.Text;
                // todo: methodSemanticModel.GetSymbolInfo(invocationChildNodes.OfType<MemberAccessExpressionSyntax>().FirstOrDefault().Expression)

                if (fieldName != null && parentClassFields.TryGetValue(fieldName, out var fieldTypeSymbol))
                {
                    switch (fieldTypeSymbol.TypeKind)
                    {
                        case TypeKind.Interface:
                        {
                            var classSyntax2 = projects
                                .SelectMany(x => x.Classes)
                                .Select(@class => (@class.ClassDeclarationSyntax, @class.SemanticModel.GetDeclaredSymbol(@class.ClassDeclarationSyntax)))
                                .FirstOrDefault(x => x.Item2?.AllInterfaces.Any(y => y.ToDisplayString() == fieldTypeSymbol.ToDisplayString()) == true);

                            if (classSyntax2 == default)
                            {
                                continue;
                            }

                            var methodSymbol2 = FindMethodImplementation(methodSemanticModel, methodInvocations, classSyntax2.Item2);

                            if (methodSymbol2 == null)
                            {
                                continue;
                            }

                            var invokedMethodSyntax = GetMethodDeclarationFromSymbol(projects, methodSymbol2);

                            if (invokedMethodSyntax != null)
                            {
                                yield return invokedMethodSyntax.Value;
                            }

                            break;
                        }
                        case TypeKind.Class:
                        {
                            var methodSymbol = FindMethodImplementation(methodSemanticModel, methodInvocations, fieldTypeSymbol)
                                ?? throw new InvalidOperationException(
                                    $"Не найден метод {methodSemanticModel} в классе {fieldTypeSymbol.ToDisplayString()}.");

                            var invokedMethodSyntax = GetMethodDeclarationFromSymbol(projects, methodSymbol)
                                ?? throw new InvalidOperationException(
                                    $"Не найден метод {methodSemanticModel} в классе {fieldTypeSymbol.ToDisplayString()}.");

                            yield return invokedMethodSyntax;

                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    var fieldName2 = invocationChildNodes.OfType<IdentifierNameSyntax>().FirstOrDefault()?.Identifier.Text;

                    var methodDeclarationSyntax = classSyntax.Members.OfType<MethodDeclarationSyntax>()
                        .FirstOrDefault(x => x.Identifier.Text == fieldName2);

                    if (methodDeclarationSyntax != null)
                    {
                        yield return (classSyntax, methodDeclarationSyntax);
                    }
                    else
                    {
                        //Console.WriteLine($"{fieldName2} - {string.Join("", invocationChildNodes.Select(x => x.ToString()))} - skipped");
                    }
                }
            }
        }
    }

    private static (ClassDeclarationSyntax, MethodDeclarationSyntax)? GetMethodDeclarationFromSymbol(
        IReadOnlyCollection<ParsedProject> projects,
        IMethodSymbol methodSymbol)
    {
        var methodLocation = methodSymbol.Locations.FirstOrDefault(x => x.IsInSource);

        var methodFilePath = methodLocation?.SourceTree?.FilePath
            ?? throw new InvalidOperationException($"Не найден путь к файлу метода {methodSymbol}");

        var document = projects.SelectMany(x => x.Classes).FirstOrDefault(x => x.Document.FilePath == methodFilePath);

        var method = document?.SyntaxRootNode.FindNode(methodLocation.SourceSpan) as MethodDeclarationSyntax;

        if (method == null)
        {
            return null;
        }
        return (document.ClassDeclarationSyntax, method);
    }

    private static IMethodSymbol? FindMethodImplementation(
        SemanticModel methodSemanticModel,
        InvocationExpressionSyntax invocation,
        INamedTypeSymbol implementingClass)
    {
        if (methodSemanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol invokedMethodSymbol)
        {
            return FindImplementationInClass(invokedMethodSymbol, implementingClass);
        }

        return null;
    }

    private static IMethodSymbol? FindImplementationInClass(IMethodSymbol interfaceMethod, INamedTypeSymbol implementingClass)
    {
        foreach (var member in implementingClass.GetMembers().OfType<IMethodSymbol>())
        {
            if (member.IsImplement(interfaceMethod))
            {
                return member;
            }
        }

        var baseType = implementingClass.BaseType;
        while (baseType != null)
        {
            foreach (var member in baseType.GetMembers().OfType<IMethodSymbol>())
            {
                if (member.IsImplement(interfaceMethod))
                {
                    return member;
                }
            }
            baseType = baseType.BaseType;
        }

        return null;
    }

    private static string ToString(MethodDeclarationSyntax method)
    {
        var className = method.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault()?.Identifier.Text;
        var parameterTypes = method.ParameterList.Parameters.Where(x => x.Type != null).Select(x => x.Type!.ToFullString().Trim());

        return $"{className}.{method.Identifier.Text}({string.Join(",", parameterTypes)})";
    }

    private static string GetTabs(int count) => $"{Enumerable.Repeat("   ", count).JoinStrings(string.Empty)}\u2514\u2500\u2500";

    private static void WriteWithTab(int depth, string text) => Console.WriteLine($"{GetTabs(depth)}{text}");
}
