using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C4ModelBuilder.Analyzer;

internal static class CqrsAnalyzer
{
    public static IEnumerable<(string, ClassDeclarationSyntax, MethodDeclarationSyntax)> GetRequestHandlersMapping(
        IReadOnlyCollection<ParsedProject> projects)
    {
        foreach (var requestClass in projects.SelectMany(x => x.Classes).Where(@class => IsCqrsRequest(@class.ClassDeclarationSyntax, @class.SemanticModel)))
        {
            foreach (var @class in projects.SelectMany(x => x.Classes))
            {
                var queryName = requestClass.ClassDeclarationSyntax.Identifier.Text;

                if (@class.ClassDeclarationSyntax.BaseList?.Types.Any(
                        type => DoesImplementCqrsHandler(queryName, type.Type, @class.SemanticModel))
                    == true)
                {
                    var method = @class.ClassDeclarationSyntax.Members
                        .OfType<MethodDeclarationSyntax>()
                        .First(x => x.Identifier.Text == "HandleAsync");

                    yield return (queryName, @class.ClassDeclarationSyntax, method);

                    break;
                }
            }
        }
    }

    private static bool IsCqrsRequest(ClassDeclarationSyntax @class, SemanticModel semanticModel)
        =>
        // @class.BaseList?.Types.Any(
        //     x => (x.Type as SimpleNameSyntax)?.Identifier.Text is "IQuery" or "ICommand" or "IResultingCommand")
        semanticModel.GetDeclaredSymbol(@class)
            ?.AllInterfaces
            .Any(x => x.Name is "IQuery" or "ICommand" or "IResultingCommand")
        == true
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
