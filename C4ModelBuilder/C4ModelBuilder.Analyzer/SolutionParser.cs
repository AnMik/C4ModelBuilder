using C4ModelBuilder.Analyzer.Infrastructure;
using C4ModelBuilder.Analyzer.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C4ModelBuilder.Analyzer;

internal static class SolutionParser
{
    public static async Task<ParsedSolution> Parse(Solution solution, CancellationToken ct = default)
    {
        var projects = await solution
                             .Projects
                             .Where(x => !x.Name.ContainsIgnoreCase("tests"))
                             .ToAsyncEnumerable()
                             .SelectAwait(async project => await ParseProject(project, ct))
                             .ToListAsync(ct);

        return new ParsedSolution(projects, CreateSemanticModels(projects), CreateInterfaceImplementationIndex(projects));
    }

    private static IReadOnlyDictionary<SyntaxTree, SemanticModel> CreateSemanticModels(
        IReadOnlyCollection<ParsedSolution.Project> projects)
    {
        var semanticModels = new Dictionary<SyntaxTree, SemanticModel>();

        foreach (var compilation in projects.Select(x => x.Compilation))
        {
            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var added = semanticModels.TryAdd(syntaxTree, compilation.GetSemanticModel(syntaxTree));

                if (!added)
                {
                    // todo: log warning
                }
            }
        }

        return semanticModels;
    }

    private static IReadOnlyDictionary<string, INamedTypeSymbol> CreateInterfaceImplementationIndex(
        IReadOnlyCollection<ParsedSolution.Project> projects)
    {
        var interfaceImplementations = new Dictionary<string, INamedTypeSymbol>(StringComparer.Ordinal);

        foreach (var @class in projects.SelectMany(project => project.Classes))
        {
            if (@class.SemanticModel.GetDeclaredSymbol(@class.ClassDeclarationSyntax) is not { } classSymbol)
            {
                continue;
            }

            foreach (var @interface in classSymbol.AllInterfaces)
            {
                var added = interfaceImplementations.TryAdd(@interface.ToDisplayString(), classSymbol);

                if (!added)
                {
                    // todo: log warning
                }
            }
        }

        return interfaceImplementations;
    }

    private static async Task<ParsedSolution.Project> ParseProject(Project project, CancellationToken ct = default)
    {
        var compilation = await project.GetCompilationAsync(ct)
            ?? throw new InvalidOperationException("Не удалось получить объект compilation.");

        var classes = await project
                            .Documents
                            .ToAsyncEnumerable()
                            .SelectAwait(async document => await ParseDocument(document, ct))
                            .SelectMany(x => x.ToAsyncEnumerable())
                            .ToListAsync(ct);

        return new ParsedSolution.Project(compilation, classes);
    }

    private static async Task<IEnumerable<ParsedSolution.Project.Class>> ParseDocument(Document document, CancellationToken ct = default)
    {
        var semanticModel = await document.GetSemanticModelAsync(ct)
            ?? throw new InvalidOperationException($"Не удалось получить семантическую модель для документа {document.Name}.");

        var syntaxRootNode = await semanticModel.SyntaxTree.GetRootAsync(ct);

        return syntaxRootNode
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Select(x => new ParsedSolution.Project.Class(semanticModel, x));
    }
}
