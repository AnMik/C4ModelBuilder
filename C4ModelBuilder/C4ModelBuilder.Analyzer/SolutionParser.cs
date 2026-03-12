using Afisha.Tickets.Core.Objects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C4ModelBuilder.Analyzer;

internal static class SolutionParser
{
    public static async Task<IReadOnlyCollection<ParsedProject>> Parse(Solution solution, CancellationToken ct = default)
        => await solution
            .Projects
            .Where(x => !x.Name.ContainsIgnoreCase("tests"))
            .ToAsyncEnumerable()
            .SelectAwait(async project => await ParseProject(project, ct))
            .ToListAsync(ct);

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
        var syntaxTree = await document.GetSyntaxTreeAsync(ct) ?? throw new InvalidOperationException("syntaxTree");
        var syntaxRootNode = await syntaxTree.GetRootAsync(ct);
        var semanticModel = await document.GetSemanticModelAsync(ct) ?? throw new InvalidOperationException("semanticModel");

        return syntaxRootNode
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Select(x => new ParsedProject.Class(document, syntaxRootNode, semanticModel, x));
    }
}
