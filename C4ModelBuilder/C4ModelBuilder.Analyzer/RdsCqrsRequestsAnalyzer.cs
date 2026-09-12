using C4ModelBuilder.Analyzer.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C4ModelBuilder.Analyzer;

internal sealed class RdsCqrsRequestsAnalyzer(ILogger logger)
{
    private const string HandleAsyncMethodName = "HandleAsync";

    public IReadOnlyDictionary<string, ClassMethod> Analyze(
        ParsedSolution parsedSolution,
        CancellationToken ct = default)
    {
        var cqrsRequestClassNames = parsedSolution
                                    .AllClasses
                                    .Where(@class => IsCqrsRequest(@class.ClassDeclarationSyntax, @class.SemanticModel))
                                    .Select(@class => @class.ClassDeclarationSyntax.Identifier.Text);

        var requestNames = new HashSet<string>(cqrsRequestClassNames, StringComparer.Ordinal);

        if (requestNames.Count == 0)
        {
            return new Dictionary<string, ClassMethod>();
        }

        var requestHandlers = new Dictionary<string, ClassMethod>(requestNames.Count, StringComparer.Ordinal);

        foreach (var @class in parsedSolution.AllClasses)
        {
            ct.ThrowIfCancellationRequested();

            if (requestHandlers.Count == requestNames.Count)
            {
                break;
            }

            if (@class.ClassDeclarationSyntax.BaseList is not { } baseList)
            {
                continue;
            }

            foreach (var baseType in baseList.Types)
            {
                var handledRequestName = TryGetRequestHandledName(baseType.Type, @class.SemanticModel);

                if (handledRequestName == null
                    || !requestNames.Contains(handledRequestName)
                    || requestHandlers.ContainsKey(handledRequestName))
                {
                    continue;
                }

                var cqrsHandlerMethod = @class
                    .ClassDeclarationSyntax
                    .Members
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault(x => x.Identifier.Text == HandleAsyncMethodName);

                if (cqrsHandlerMethod == null)
                {
                    logger.LogWarning(
                        "CQRS-хендлер {handler} для {request} не содержит метод HandleAsync — класс пропущен.",
                        @class.ClassDeclarationSyntax.Identifier.Text,
                        handledRequestName);

                    continue;
                }

                var added = requestHandlers.TryAdd(handledRequestName, new ClassMethod(@class.ClassDeclarationSyntax, cqrsHandlerMethod));

                if (!added)
                {
                    logger.LogInformation(
                        "CQRS-хендлер {handler} для {request} уже есть в словаре — класс пропущен.",
                        @class.ClassDeclarationSyntax.Identifier.Text,
                        handledRequestName);
                }
            }
        }

        return requestHandlers;
    }

    private bool IsCqrsRequest(ClassDeclarationSyntax @class, SemanticModel semanticModel)
        => semanticModel.GetDeclaredSymbol(@class)?.AllInterfaces.Any(x => x.Name is "IQuery" or "ICommand" or "IResultingCommand") == true
            && @class.Modifiers.All(
                syntaxToken => !syntaxToken.IsKind(SyntaxKind.StructKeyword) && !syntaxToken.IsKind(SyntaxKind.PrivateKeyword));

    private string? TryGetRequestHandledName(TypeSyntax typeSyntax, SemanticModel semanticModel)
    {
        if (semanticModel.GetTypeInfo(typeSyntax).Type is not INamedTypeSymbol { IsGenericType: true } typeSymbol)
        {
            return null;
        }

        return IsCqrsHandler(typeSymbol.OriginalDefinition)
            ? typeSymbol.TypeArguments.FirstOrDefault()?.Name
            : null;
    }

    private bool IsCqrsHandler(INamedTypeSymbol originalDefinition)
    {
        var containingNamespace = originalDefinition.ContainingNamespace;

        return originalDefinition.Name switch
        {
            "IQueryHandler" => originalDefinition.Arity == 2 && IsCqrsNamespace(containingNamespace, "Queries"),
            "ICommandHandler" => originalDefinition.Arity == 1 && IsCqrsNamespace(containingNamespace, "Commands"),
            "IResultingCommandHandler" => originalDefinition.Arity == 2 && IsCqrsNamespace(containingNamespace, "Commands"),
            _ => false,
        };
    }

    private bool IsCqrsNamespace(INamespaceSymbol? namespaceSymbol, string leaf)
    {
        if (namespaceSymbol is null || namespaceSymbol.Name != leaf)
        {
            return false;
        }

        var parentNamespace = namespaceSymbol.ContainingNamespace;
        if (parentNamespace is null || parentNamespace.Name != "Cqrs")
        {
            return false;
        }

        return parentNamespace.ContainingNamespace is
        {
            IsGlobalNamespace: false,
            Name: "Rds",
            ContainingNamespace.IsGlobalNamespace: true
        };
    }
}
