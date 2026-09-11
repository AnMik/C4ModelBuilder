using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C4ModelBuilder.Analyzer.Models;

internal sealed record ParsedSolution
{
    internal sealed record Project(Compilation Compilation, IReadOnlyCollection<Project.Class> Classes)
    {
        public sealed record Class(
            Document Document,
            SyntaxNode SyntaxRootNode,
            SemanticModel SemanticModel,
            ClassDeclarationSyntax ClassDeclarationSyntax);
    }

    private readonly IReadOnlyCollection<Project> _projects;
    private readonly IReadOnlyDictionary<SyntaxTree, SemanticModel> _semanticModels;

    public IEnumerable<Project.Class> AllClasses => _projects.SelectMany(x => x.Classes);

    public IEnumerable<INamedTypeSymbol> AllSymbols
        => AllClasses.Select(x => x.SemanticModel.GetDeclaredSymbol(x.ClassDeclarationSyntax)).OfType<INamedTypeSymbol>();

    public ParsedSolution(IReadOnlyCollection<Project> projects, IReadOnlyDictionary<SyntaxTree, SemanticModel> semanticModels)
    {
        _projects = projects;
        _semanticModels = semanticModels;
    }

    public ClassMethod? FindMethodDeclaration(IMethodSymbol methodSymbol)
    {
        var methodLocation = methodSymbol.Locations.FirstOrDefault(x => x.IsInSource);

        // Метод не объявлен в исходниках (библиотечный).
        if (methodLocation?.SourceTree?.FilePath is not { } methodFilePath)
        {
            return null;
        }

        // Класс ищем по вхождению метода, т.к. в одном документе может быть несколько классов.
        var declaredClass = AllClasses
            .FirstOrDefault(
                x => x.Document.FilePath == methodFilePath && x.ClassDeclarationSyntax.Span.Contains(methodLocation.SourceSpan));

        return declaredClass?.SyntaxRootNode.FindNode(methodLocation.SourceSpan) is MethodDeclarationSyntax methodSyntax
            ? new ClassMethod(declaredClass.ClassDeclarationSyntax, methodSyntax)
            : null;
    }

    public SemanticModel GetSemanticModel(SyntaxTree syntaxTree)
        => _semanticModels.GetValueOrDefault(syntaxTree)
            ?? throw new InvalidOperationException($"Не найдена семантическая модель для синтаксического дерева {syntaxTree.FilePath}.");

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
