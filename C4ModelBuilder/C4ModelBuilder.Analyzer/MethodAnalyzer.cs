using C4ModelBuilder.Analyzer.Infrastructure;
using C4ModelBuilder.Analyzer.Models;
using C4ModelBuilder.Models.Analysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C4ModelBuilder.Analyzer;

internal sealed class MethodAnalyzer(
    ILogger logger,
    ParsedSolution parsedSolution,
    IReadOnlyDictionary<string, ClassMethod> rdsCqrsRequests,
    int maxDepth)
{
    public async Task<InvocationTree?> AnalyzeMethod(ClassMethod classMethod, int currentDepth, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (currentDepth > maxDepth)
        {
            logger.LogDebug("Вызов {method} пропущен: достигнут лимит глубины {maxDepth}.", classMethod.Name, maxDepth);

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
        var parentClassFields = classMethod
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

                var requestName = baseRequest
                                  .Concat(genericRequest)
                                  .Concat(genericRequestWithReadPreference)
                                  .FirstOrDefault()
                                  ?.Identifier.Text;

                if (requestName == null)
                {
                    logger.LogWarning(
                        "Не удалось определить имя CQRS-реквеста в вызове внутри {method} — вызов пропущен.",
                        classMethod.Name);

                    continue;
                }

                var handlerTypedSymbol = rdsCqrsRequests.GetValueOrDefault(requestName);

                if (handlerTypedSymbol != null)
                {
                    var (classSymbol, methodSymbol) = parsedSolution.GetDeclaredSymbols(handlerTypedSymbol);

                    yield return InvokedMethod.From(handlerTypedSymbol, classSymbol, methodSymbol);

                    continue;
                }

                var requestSymbol = parsedSolution.AllSymbols.FirstOrDefault(symbol => symbol.Name == requestName);

                if (requestSymbol == null)
                {
                    logger.LogWarning(
                        "Не найден символ CQRS-реквеста {request} в вызове внутри {method} — вызов пропущен.",
                        requestName,
                        classMethod.Name);

                    continue;
                }

                logger.LogWarning(
                    "Не найден обработчик CQRS-реквеста {request} (вызов в {method}); реквест добавлен без обработчика.",
                    requestName,
                    classMethod.Name);

                yield return InvokedMethod.From(classSymbol: requestSymbol, methodSymbol: null);
            }
            else
            {
                var fieldName = methodInvocation
                                .ChildNodes()
                                .OfType<MemberAccessExpressionSyntax>()
                                .Select(x => x.Expression)
                                .OfType<IdentifierNameSyntax>()
                                .FirstOrDefault()
                                ?.Identifier.Text;

                if (fieldName == null)
                {
                    var methodInvocationSyntaxName = methodInvocation
                                                     .ChildNodes()
                                                     .OfType<IdentifierNameSyntax>()
                                                     .FirstOrDefault()
                                                     ?.Identifier.Text;

                    var methodDeclarationSyntax = classMethod
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
                    else
                    {
                        logger.LogDebug(
                            "Вызов {call} внутри {method} не привязан к полю и метод не найден в классе — вызов пропущен.",
                            methodInvocationSyntaxName,
                            classMethod.Name);
                    }
                }
                else
                {
                    if (!parentClassFields.TryGetValue(fieldName, out var fieldTypeSymbol))
                    {
                        logger.LogDebug(
                            "Поле {field} в {method} пропущено: тип не является анализируемым классом/интерфейсом.",
                            fieldName,
                            classMethod.Name);

                        continue;
                    }

                    switch (fieldTypeSymbol.TypeKind)
                    {
                        case TypeKind.Interface:
                        {
                            var implementingClassSyntax = parsedSolution
                                                          .AllSymbols
                                                          .FirstOrDefault(
                                                              classSymbol => classSymbol.AllInterfaces.Any(
                                                                  x => x.ToDisplayString() == fieldTypeSymbol.ToDisplayString()));

                            if (implementingClassSyntax == null)
                            {
                                logger.LogDebug(
                                    "Для интерфейса {interface} не найдена реализация — добавлен интерфейс без реализации.",
                                    fieldTypeSymbol.ToDisplayString());

                                yield return InvokedMethod.From(
                                    classSymbol: fieldTypeSymbol,
                                    methodSymbol: methodSemanticModel.GetSymbolInfo(methodInvocation).Symbol as IMethodSymbol);

                                break;
                            }

                            var methodSymbol = methodSemanticModel.FindMethodImplementation(implementingClassSyntax, methodInvocation);

                            if (methodSymbol == null)
                            {
                                logger.LogWarning(
                                    "Не найдена реализация метода вызова {call} в {method} — вызов пропущен.",
                                    methodInvocation.ToString(),
                                    classMethod.Name);

                                continue;
                            }

                            var invokedMethodSyntax = parsedSolution.FindMethodDeclaration(methodSymbol);

                            if (invokedMethodSyntax == null)
                            {
                                logger.LogWarning(
                                    "Не найдено объявление метода {method} в исходниках — вызов пропущен.",
                                    methodSymbol.ToDisplayString());

                                break;
                            }

                            var (classSymbol, _) = parsedSolution.GetDeclaredSymbols(invokedMethodSyntax);

                            yield return InvokedMethod.From(invokedMethodSyntax, classSymbol, methodSymbol);

                            break;
                        }
                        case TypeKind.Class:
                        {
                            var methodSymbol = methodSemanticModel.FindMethodImplementation(fieldTypeSymbol, methodInvocation);

                            if (methodSymbol == null)
                            {
                                logger.LogWarning(
                                    "Не найден метод вызова {call} в классе {class} — вызов пропущен.",
                                    methodInvocation.ToString(),
                                    fieldTypeSymbol.ToDisplayString());

                                continue;
                            }

                            var invokingClassMethod = parsedSolution.FindMethodDeclaration(methodSymbol);

                            if (invokingClassMethod == null)
                            {
                                logger.LogWarning(
                                    "Не найдено объявление метода {method} в исходниках — вызов пропущен.",
                                    methodSymbol.ToDisplayString());

                                break;
                            }

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
