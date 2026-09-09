using C4ModelBuilder.Analyzer.Models;
using C4ModelBuilder.Models.Analysis;
using C4ModelBuilder.Models.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace C4ModelBuilder.Analyzer;

public static class SolutionAnalyzer
{
    public static async Task<IReadOnlyCollection<C4ComponentDiagram>> AnalyzeComponents(
        string solutionPath,
        int maxDepth = 15,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var workspace = MSBuildWorkspace.Create();
        var solution = await workspace.OpenSolutionAsync(solutionPath, cancellationToken: cancellationToken);

        var parsedSolution = await SolutionParser.Parse(solution, cancellationToken);

        var requestHandlersMapping = CqrsRequestsAnalyzer
            .Analyze(parsedSolution, cancellationToken)
            .GroupBy(
                x => x.Name,
                (x, items) => (x, RequestHandlerClass: items.First().HandlerClass, RequestHandlerMethod: items.First().HandlerMethod))
            .ToDictionary(x => x.x, x => (x.Item2, x.Item3));

        var methodAnalyzer = new MethodAnalyzer(parsedSolution, requestHandlersMapping, maxDepth);

        var rootMemberNode = new MemberNode("Root");

        foreach (var @class in parsedSolution.Projects.SelectMany(x => x.Classes))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var classSyntax = @class.ClassDeclarationSyntax;

            var methodSyntaxes = classSyntax
                .Members
                .OfType<MethodDeclarationSyntax>()
                .Where(
                    method => method
                        .AttributeLists
                        .SelectMany(x => x.Attributes)
                        .Any(x => x.Name.ToString() == C4ComponentAttribute.Name));

            foreach (var methodSyntax in methodSyntaxes)
            {
                var memberNode = await methodAnalyzer.AnalyzeMethod(classSyntax, methodSyntax, currentDepth: 0, cancellationToken);
                if (memberNode != null)
                {
                    var classNode = new MemberNode(classSyntax.Identifier.Text);
                    classNode.AddChild(memberNode);
                    rootMemberNode.AddChild(classNode);
                }
            }
        }

        MethodAnalyzer.WriteHierarchy(rootMemberNode);

        var plantUmlContext = C4ComponentDiagramBuilder.Build(rootMemberNode, cancellationToken);

        return [plantUmlContext];
    }
}
