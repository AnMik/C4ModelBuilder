using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C4ModelBuilder.Analyzer.Models;

internal sealed record ParsedSolution(IReadOnlyCollection<ParsedSolution.Project> Projects)
{
    internal sealed record Project(Compilation Compilation, IReadOnlyCollection<Project.Class> Classes)
    {
        public sealed record Class(
            Document Document,
            SyntaxNode SyntaxRootNode,
            SemanticModel SemanticModel,
            ClassDeclarationSyntax ClassDeclarationSyntax);
    }

    public ClassMethod? FindMethodDeclaration(IMethodSymbol methodSymbol)
    {
        var methodLocation = methodSymbol.Locations.FirstOrDefault(x => x.IsInSource);

        var methodFilePath = methodLocation?.SourceTree?.FilePath
            ?? throw new InvalidOperationException($"Не найден путь к файлу метода {methodSymbol}.");

        var document = Projects.SelectMany(x => x.Classes).FirstOrDefault(x => x.Document.FilePath == methodFilePath);

        if (document?.SyntaxRootNode.FindNode(methodLocation.SourceSpan) is not MethodDeclarationSyntax methodSyntax)
        {
            return null;
        }

        return new(document.ClassDeclarationSyntax, methodSyntax);
    }

    public SemanticModel GetSemanticModel(SyntaxTree syntaxTree)
        => Projects
                .Where(project => project.Compilation.SyntaxTrees.Contains(syntaxTree))
                .Select(project => project.Compilation.GetSemanticModel(syntaxTree))
                .FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"Не найдена семантическая модель для синтаксического дерева {syntaxTree.FilePath}.");

    public (INamedTypeSymbol ClassSymbol, IMethodSymbol? MethodSymbol) GetDeclaredSymbols(ClassMethod classMethod)
    {
        var semanticModel = GetSemanticModel(classMethod.ClassSyntax.SyntaxTree);

        return (
            ClassSymbol: semanticModel.GetDeclaredSymbol(classMethod.ClassSyntax)
            ?? throw new InvalidOperationException($"Не найден символ класса {classMethod.ClassSyntax.Identifier.Text}."),
            MethodSymbol: semanticModel.GetDeclaredSymbol(classMethod.MethodSyntax)
            ?? throw new InvalidOperationException($"Не найден символ метода {classMethod.MethodSyntax.Identifier.Text}."));
    }
}
