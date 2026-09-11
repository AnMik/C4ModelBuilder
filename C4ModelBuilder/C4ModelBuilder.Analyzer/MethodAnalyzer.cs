using C4ModelBuilder.Analyzer.Infrastructure;
using C4ModelBuilder.Analyzer.Models;
using C4ModelBuilder.Models.Analysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C4ModelBuilder.Analyzer;

internal sealed class MethodAnalyzer(ParsedSolution parsedSolution, Dictionary<string, ClassMethod> rdsCqrsRequests, int maxDepth)
{
    public async Task<InvocationTree?> AnalyzeMethod(ClassMethod classMethod, int currentDepth, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (currentDepth > maxDepth)
        {
            return null;
        }

        var methodSemanticModel = parsedSolution.GetSemanticModel(classMethod.MethodSyntax.SyntaxTree);

        var currentMethodNode = new InvocationTree(
            nodeName: classMethod.Name,
            c4ComponentDescription: methodSemanticModel
                .GetDeclaredSymbol(classMethod.MethodSyntax)
                ?.GetC4ComponentAttribute()
                .GetC4ComponentDescription());

        foreach (var invokedMethod in GetInvokedMethods(classMethod, methodSemanticModel))
        {
            var invokedClassNode = new InvocationTree(
                nodeName: invokedMethod.ClassName,
                c4ComponentDescription: invokedMethod.ClassDescription);

            var invokedMethodNode = invokedMethod.ClassMethod != null
                ? await AnalyzeMethod(invokedMethod.ClassMethod, currentDepth + 1, ct)
                : new InvocationTree(
                    nodeName: $"{invokedMethod.ClassName}.{invokedMethod.MethodName}",
                    c4ComponentDescription: invokedMethod.MethodDescription);

            if (invokedMethodNode != null)
            {
                invokedClassNode.AddInvocation(invokedMethodNode);
                currentMethodNode.AddInvocation(invokedClassNode);
            }
        }

        return currentMethodNode;
    }

    private IEnumerable<InvokedMethod> GetInvokedMethods(ClassMethod classMethod, SemanticModel methodSemanticModel)
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
                    x => x.FieldType is { TypeKind: TypeKind.Class or TypeKind.Interface } fieldType
                        && fieldType.Name is not ("IMapper" or "ITaggableCache")
                        && fieldType.OriginalDefinition.Locations.Any(location => location.IsInSource))
                .ToDictionary(x => x.FieldName, x => x.FieldType!);

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

                var handlerTypedSymbol = rdsCqrsRequests.GetValueOrDefault(queryName)
                    ?? throw new InvalidOperationException($"Не найден cqrs хендлер для {queryName}.");

                var (classSymbol, methodSymbol) = parsedSolution.GetDeclaredSymbols(handlerTypedSymbol);

                yield return InvokedMethod.From(handlerTypedSymbol, classSymbol, methodSymbol);
            }
            else
            {
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
                        var methodClassMethod = classMethod with { MethodSyntax = methodDeclarationSyntax };
                        var (classSymbol, methodSymbol) = parsedSolution.GetDeclaredSymbols(methodClassMethod);

                        yield return InvokedMethod.From(methodClassMethod, classSymbol, methodSymbol);
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
                                // Реализация интерфейса не найдена, добавляется интерфейс.
                                yield return InvokedMethod.From(
                                    classSymbol: fieldTypeSymbol,
                                    methodSymbol: methodSemanticModel.GetSymbolInfo(methodInvocation).Symbol as IMethodSymbol);

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
                                var (classSymbol, _) = parsedSolution.GetDeclaredSymbols(invokedMethodSyntax);

                                yield return InvokedMethod.From(invokedMethodSyntax, classSymbol, methodSymbol);
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

                            var (classSymbol, _) = parsedSolution.GetDeclaredSymbols(invokingClassMethod);

                            yield return InvokedMethod.From(invokingClassMethod, classSymbol, methodSymbol);

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
