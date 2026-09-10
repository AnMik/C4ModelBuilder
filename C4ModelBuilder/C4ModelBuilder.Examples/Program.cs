using System.Diagnostics;
using C4ModelBuilder.Analyzer;
using C4ModelBuilder.PlantUmlCreator;

Console.WriteLine("Started.");
var sw = new Stopwatch();
sw.Start();
using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) => CancelToken(eventArgs, cancellationTokenSource);

try
{
    await Run(cancellationTokenSource);
}
catch (TaskCanceledException)
{
    Console.WriteLine("Canceled.");
    return;
}

Console.WriteLine($"Finished ({sw.Elapsed:mm\\:ss}).");
return;

async Task Run(CancellationTokenSource cts)
{
    // var solutionFilePath = "C:\\Repos\\kassa\\Afisha.Tickets.All.sln";
    var solutionDirectory = GetCurrentSolutionPath();
    var solutionFilePath = Path.Combine(solutionDirectory.FullName, "C4ModelBuilder.sln");

    var solutionAnalyzer = await SolutionAnalyzer.Create(solutionFilePath, maxDepth: 15, cts.Token);
    var componentDiagrams = solutionAnalyzer.AnalyzeComponents(cts.Token);

    var i = 1;
    await foreach (var componentDiagram in componentDiagrams)
    {
        var plantUml = PlantUmlGenerator.Generate(componentDiagram);

        var path = Path.Combine(solutionDirectory.Parent!.FullName, "output", $"uml{i++}.puml");
        await File.WriteAllTextAsync(path, plantUml);
        Console.WriteLine(path);
    }
}

void CancelToken(ConsoleCancelEventArgs args, CancellationTokenSource cts)
{
    args.Cancel = true;
    try
    {
        cts.Cancel();
    }
    catch (ObjectDisposedException)
    {
        // ignore
    }
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
