using Afisha.Tickets.Core.Objects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace С4ModelBuilder;

public static class SolutionParser
{
    public static async Task<IReadOnlyCollection<ParsedProject>> Parse(Solution solution, CancellationToken ct = default)
        => await solution
            .Projects
            .Where(x => !x.Name.ContainsIgnoreCase("tests"))
            // .Where(x => x.Name.Contains("MobileApi"))
            .ToAsyncEnumerable()
            .SelectAwait(async project => await ParseProject(project))
            .ToListAsync(ct);

    private static async Task<ParsedProject> ParseProject(Project project, CancellationToken ct = default)
    {
        var compilation = await project.GetCompilationAsync(ct) ?? throw new Exception("compilation");
        var docs = await project.Documents.ToAsyncEnumerable().SelectAwait(async document => await ParseDocument(document, ct)).ToListAsync(ct);

        return new ParsedProject(project, compilation, docs);
    }

    private static async Task<ParsedProject.Doc> ParseDocument(Document document, CancellationToken ct = default)
    {
        var syntaxTree = await document.GetSyntaxTreeAsync(ct) ?? throw new Exception("syntaxTree");
        var syntaxRootNode = await syntaxTree.GetRootAsync(ct);
        var semanticModel = await document.GetSemanticModelAsync(ct) ?? throw new Exception("semanticModel");
        var classDeclarationSyntaxes = syntaxRootNode.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();

        return new ParsedProject.Doc(document, syntaxTree, syntaxRootNode, semanticModel, classDeclarationSyntaxes);
    }
}
