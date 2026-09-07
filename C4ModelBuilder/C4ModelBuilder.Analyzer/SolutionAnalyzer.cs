using C4ModelBuilder.Analyzer.Models;
using C4ModelBuilder.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace C4ModelBuilder.Analyzer;

public static class SolutionAnalyzer
{
    public static async Task<PlantUmlC4ComponentDiagram> Analyze(string solutionPath, int maxDepth = 15)
    {
        using var workspace = MSBuildWorkspace.Create();
        var solution = await workspace.OpenSolutionAsync(solutionPath);

        var parsedSolution = await SolutionParser.Parse(solution);
        var requestHandlersMapping = CqrsAnalyzer
            .GetRequestHandlersMapping(parsedSolution)
            .GroupBy(x => x.Item1, (x, y) => (x, y.First().Item2, y.First().Item3))
            .ToDictionary(x => x.x, x => (x.Item2, x.Item3));

        var componentAttributeName = nameof(C4ComponentAttribute)[..^(nameof(Attribute).Length)];

        var methodAnalyzer = new MethodAnalyzer(parsedSolution, requestHandlersMapping, maxDepth);

        var memberNode = new MemberNode("Root");

        foreach (var doc in parsedSolution.Projects.SelectMany(x => x.Classes))
        {
            var @class = doc.ClassDeclarationSyntax;

            var mapiMethodsWithAttribute = @class
                .Members
                .OfType<MethodDeclarationSyntax>()
                .Where(
                    method => method
                        .AttributeLists
                        .SelectMany(attributeList => attributeList.Attributes)
                        .Any(attribute => attribute.Name.ToString() == componentAttributeName));

            foreach (var method in mapiMethodsWithAttribute)
            {
                var node = await methodAnalyzer.AnalyzeMethod(@class, method, currentDepth: 0);
                if (node != null)
                {
                    memberNode.AddChild(node);
                }
            }
        }

        var plantUmlContext = PlantUmlContextBuilder.Build(memberNode);

        MethodAnalyzer.WriteHierarchy(memberNode);

        return plantUmlContext;
    }
}
