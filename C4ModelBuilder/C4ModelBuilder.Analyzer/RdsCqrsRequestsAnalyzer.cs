using C4ModelBuilder.Analyzer.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace C4ModelBuilder.Analyzer;

internal static class RdsCqrsRequestsAnalyzer
{
    public static IReadOnlyDictionary<string, ClassMethod> Analyze(
        ParsedSolution parsedSolution,
        ILogger logger,
        CancellationToken ct = default)
    {
        var requestHandlers = new Dictionary<string, ClassMethod>();

        var cqrsRequestClasses = parsedSolution
            .Projects
            .SelectMany(x => x.Classes)
            .Where(@class => IsCqrsRequest(@class.ClassDeclarationSyntax, @class.SemanticModel));

        foreach (var cqrsRequestClass in cqrsRequestClasses)
        {
            foreach (var @class in parsedSolution.GetAllClasses())
            {
                ct.ThrowIfCancellationRequested();

                var cqrsRequestName = cqrsRequestClass.ClassDeclarationSyntax.Identifier.Text;

                if (@class.ClassDeclarationSyntax.BaseList?.Types.Any(
                        type => DoesImplementCqrsHandler(cqrsRequestName, type.Type, @class.SemanticModel))
                    != true)
                {
                    continue;
                }

                var cqrsHandlerMethod = @class
                    .ClassDeclarationSyntax
                    .Members
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault(x => x.Identifier.Text == "HandleAsync");

                if (cqrsHandlerMethod == null)
                {
                    logger.LogWarning(
                        "CQRS-хендлер {handler} для {request} не содержит метод HandleAsync — класс пропущен.",
                        @class.ClassDeclarationSyntax.Identifier.Text,
                        cqrsRequestName);

                    continue;
                }

                requestHandlers.TryAdd(cqrsRequestName, new ClassMethod(@class.ClassDeclarationSyntax, cqrsHandlerMethod));

                break;
            }
        }

        return requestHandlers;
    }

    private static bool IsCqrsRequest(ClassDeclarationSyntax @class, SemanticModel semanticModel)
        => semanticModel.GetDeclaredSymbol(@class)?.AllInterfaces.Any(x => x.Name is "IQuery" or "ICommand" or "IResultingCommand") == true
            && @class.Modifiers.All(
                syntaxToken => !syntaxToken.IsKind(SyntaxKind.StructKeyword) && !syntaxToken.IsKind(SyntaxKind.PrivateKeyword));

    private static bool DoesImplementCqrsHandler(string requestName, TypeSyntax typeSyntax, SemanticModel semanticModel)
        => semanticModel.GetTypeInfo(typeSyntax).Type is INamedTypeSymbol { IsGenericType: true } typeSymbol
            && typeSymbol.OriginalDefinition.ToString()
                is "Rds.Cqrs.Queries.IQueryHandler<TQuery, TResult>"
                or "Rds.Cqrs.Commands.ICommandHandler<TCommand>"
                or "Rds.Cqrs.Commands.IResultingCommandHandler<TCommand, TResult>"
            && typeSymbol.TypeArguments.Any(argument => argument.Name == requestName);
}
