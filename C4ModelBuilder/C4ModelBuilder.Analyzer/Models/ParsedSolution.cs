using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C4ModelBuilder.Analyzer.Models;

internal sealed record ParsedSolution
{
    internal sealed record Project(Compilation Compilation, IReadOnlyCollection<Project.Class> Classes)
    {
        public sealed record Class(SemanticModel SemanticModel, ClassDeclarationSyntax ClassDeclarationSyntax);
    }

    private readonly IReadOnlyCollection<Project> _projects;
    private readonly IReadOnlyDictionary<SyntaxTree, SemanticModel> _semanticModels;
    private readonly IReadOnlyDictionary<string, INamedTypeSymbol> _interfaceImplementations;

    public IEnumerable<Project.Class> AllClasses => _projects.SelectMany(x => x.Classes);

    public IEnumerable<INamedTypeSymbol> AllSymbols
        => AllClasses.Select(x => x.SemanticModel.GetDeclaredSymbol(x.ClassDeclarationSyntax)).OfType<INamedTypeSymbol>();

    public ParsedSolution(
        IReadOnlyCollection<Project> projects,
        IReadOnlyDictionary<SyntaxTree, SemanticModel> semanticModels,
        IReadOnlyDictionary<string, INamedTypeSymbol> interfaceImplementations)
    {
        _projects = projects;
        _semanticModels = semanticModels;
        _interfaceImplementations = interfaceImplementations;
    }

    /// <summary>
    /// Возвращает первый класс разобранного солюшена, реализующий указанный интерфейс, либо <c>null</c>, если реализация не найдена.
    /// </summary>
    public INamedTypeSymbol? FindInterfaceImplementation(INamedTypeSymbol interfaceSymbol)
        => _interfaceImplementations.GetValueOrDefault(interfaceSymbol.ToDisplayString());

    public ClassMethod? FindMethodDeclaration(IMethodSymbol methodSymbol)
    {
        // Метод не объявлен в исходниках (библиотечный) либо объявлен вне разобранного солюшена.
        if (methodSymbol.DeclaringSyntaxReferences.FirstOrDefault() is not { } declaringSyntaxReference
            || !_semanticModels.ContainsKey(declaringSyntaxReference.SyntaxTree))
        {
            return null;
        }

        // Класс берём из объявления метода, т.к. в одном документе может быть несколько классов.
        return declaringSyntaxReference.GetSyntax() is MethodDeclarationSyntax methodSyntax
            && methodSyntax.FirstAncestorOrSelf<ClassDeclarationSyntax>() is { } classSyntax
                ? new ClassMethod(classSyntax, methodSyntax)
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
