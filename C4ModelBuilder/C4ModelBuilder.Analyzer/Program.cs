using Afisha.Tickets.Core.C4;
using C4ModelBuilder.Analyzer;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

using var workspace = MSBuildWorkspace.Create();
var solution = await workspace.OpenSolutionAsync("C:\\Repos\\kassa\\Afisha.Tickets.All.sln");

var parsedProjects = await SolutionParser.Parse(solution);
var requestHandlersMapping = CqrsAnalyzer.GetRequestHandlersMapping(parsedProjects).GroupBy(x => x.Item1, (x, y) => (x, y.First().Item2, y.First().Item3)).ToDictionary(x => x.x, x => (x.Item2, x.Item3));

var componentAttributeName = nameof(C4ComponentAttribute)[..^(nameof(Attribute).Length)];

var methodAnalyzer = new MethodAnalyzer(parsedProjects, requestHandlersMapping);

foreach (var doc in parsedProjects.SelectMany(x => x.Classes))
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
        await methodAnalyzer.AnalyzeMethod(@class, method, depth: 0);
    }
}

Console.WriteLine("Finished");
