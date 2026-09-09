using C4ModelBuilder.Analyzer;
using C4ModelBuilder.PlantUmlCreator;

// var solutionFilePath = "C:\\Repos\\kassa\\Afisha.Tickets.All.sln";
var solutionDirectory = GetCurrentSolutionPath();
var solutionFilePath = Path.Combine(solutionDirectory.FullName, "C4ModelBuilder.sln");

var componentDiagrams = await SolutionAnalyzer.AnalyzeComponents(solutionFilePath);

var i = 1;
foreach (var componentDiagram in componentDiagrams)
{
    var diagram = PlantUmlGenerator.Generate(componentDiagram);
    Console.WriteLine(diagram);

    await File.WriteAllTextAsync(Path.Combine(solutionDirectory.Parent!.FullName, "output", $"uml{i++}.puml"), diagram);
}

static DirectoryInfo GetCurrentSolutionPath()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);

    while (directory != null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "C4ModelBuilder.sln")))
        {
            return directory;
        }

        directory = directory.Parent;
    }

    throw new InvalidOperationException("Каталог решения C4ModelBuilder.sln не найден.");
}
