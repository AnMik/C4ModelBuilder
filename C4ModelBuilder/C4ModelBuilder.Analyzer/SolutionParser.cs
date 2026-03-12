using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C4ModelBuilder.Analyzer;

internal static class SolutionParser
{
    public static async Task<ParsedSolution> Parse(Solution solution, CancellationToken ct = default)
        => new(
            await solution
                .Projects
                .Where(x => !x.Name.ContainsIgnoreCase("tests"))
                .ToAsyncEnumerable()
                .SelectAwait(async project => await ParseProject(project, ct))
                .ToListAsync(ct));

    private static async Task<ParsedProject> ParseProject(Project project, CancellationToken ct = default)
    {
        var compilation = await project.GetCompilationAsync(ct) ?? throw new InvalidOperationException("compilation");
        var classes = await project
            .Documents
            .ToAsyncEnumerable()
            .SelectAwait(async document => await ParseDocument(document, ct))
            .SelectMany(x => x.ToAsyncEnumerable())
            .ToListAsync(ct);

        return new ParsedProject(compilation, classes);
    }

    private static async Task<IEnumerable<ParsedProject.Class>> ParseDocument(Document document, CancellationToken ct = default)
    {
        var syntaxTree = await document.GetSyntaxTreeAsync(ct)
            ?? throw new InvalidOperationException($"Не удалось получить синтаксическое дерево для документа {document.Name}");
        var syntaxRootNode = await syntaxTree.GetRootAsync(ct);
        var semanticModel = await document.GetSemanticModelAsync(ct)
            ?? throw new InvalidOperationException($"Не удалось получить семантическую модель для документа {document.Name}");

        return syntaxRootNode
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Select(x => new ParsedProject.Class(document, syntaxRootNode, semanticModel, x));
    }
}
