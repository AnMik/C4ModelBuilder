using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C4ModelBuilder.Analyzer;

internal static class CqrsAnalyzer
{
    public static IEnumerable<(string, ClassDeclarationSyntax, MethodDeclarationSyntax)> GetRequestHandlersMapping(
        ParsedSolution parsedSolution)
    {
        var cqrsRequestClasses = parsedSolution
            .Projects
            .SelectMany(x => x.Classes)
            .Where(@class => IsCqrsRequest(@class.ClassDeclarationSyntax, @class.SemanticModel));

        foreach (var cqrsRequestClass in cqrsRequestClasses)
        {
            foreach (var @class in parsedSolution.Projects.SelectMany(x => x.Classes))
            {
                var cqrsRequestName = cqrsRequestClass.ClassDeclarationSyntax.Identifier.Text;

                if (@class.ClassDeclarationSyntax.BaseList?.Types.Any(
                        type => DoesImplementCqrsHandler(cqrsRequestName, type.Type, @class.SemanticModel))
                    != true)
                {
                    continue;
                }

                var cqrsHandlerMethod = @class.ClassDeclarationSyntax.Members
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault(x => x.Identifier.Text == "HandleAsync")
                    ?? throw new InvalidOperationException("В cqrs хендлере не найден метод HandleAsync().");

                yield return (cqrsRequestName, @class.ClassDeclarationSyntax, cqrsHandlerMethod);
                break;
            }
        }
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
