using C4ModelBuilder.Analyzer;
using C4ModelBuilder.PlantUmlCreator;

var solutionFilePath = "C:\\Repos\\kassa\\Afisha.Tickets.All.sln";
var resultFolderPath = "C:\\Work\\C4";

var componentDiagrams = await SolutionAnalyzer.AnalyzeComponents(solutionFilePath);

var i = 1;
foreach (var componentDiagram in componentDiagrams)
{
    var diagram = PlantUmlGenerator.Generate(componentDiagram);
    Console.WriteLine(diagram);

    await File.WriteAllTextAsync(Path.Combine(resultFolderPath, $"uml{i++}.puml"), diagram);
}
