using System.Diagnostics;
using C4ModelBuilder.Analyzer;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

var sw = new Stopwatch();
sw.Start();

using var workspace = MSBuildWorkspace.Create();
var solution = await workspace.OpenSolutionAsync("C:\\Repos\\kassa\\Afisha.Tickets.All.sln");

var parsedSolution = await SolutionParser.Parse(solution);
var requestHandlersMapping = CqrsAnalyzer
    .GetRequestHandlersMapping(parsedSolution)
    .GroupBy(x => x.Item1, (x, y) => (x, y.First().Item2, y.First().Item3))
    .ToDictionary(x => x.x, x => (x.Item2, x.Item3));

var componentAttributeName = nameof(C4ComponentAttribute)[..^(nameof(Attribute).Length)];

var methodAnalyzer = new MethodAnalyzer(parsedSolution, requestHandlersMapping, maxDepth: 15);

var root = new MemberNode("Root");

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
            root.AddChild(node);
        }
    }
}

MethodAnalyzer.WriteHierarchy(root);

Console.WriteLine($"Finished: {sw.Elapsed}.");
