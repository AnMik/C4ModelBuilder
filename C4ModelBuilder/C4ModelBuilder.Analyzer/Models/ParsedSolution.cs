using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C4ModelBuilder.Analyzer.Models;

internal sealed record ParsedSolution(IReadOnlyCollection<ParsedProject> Projects)
{
    public ClassMethod? FindMethodDeclaration(IMethodSymbol methodSymbol)
    {
        var methodLocation = methodSymbol.Locations.FirstOrDefault(x => x.IsInSource);

        var methodFilePath = methodLocation?.SourceTree?.FilePath
            ?? throw new InvalidOperationException($"Не найден путь к файлу метода {methodSymbol}");

        var document = Projects.SelectMany(x => x.Classes).FirstOrDefault(x => x.Document.FilePath == methodFilePath);

        if (document?.SyntaxRootNode.FindNode(methodLocation.SourceSpan) is not MethodDeclarationSyntax methodSyntax)
        {
            return null;
        }

        return new(document.ClassDeclarationSyntax, methodSyntax);
    }
}
