using System.Diagnostics;
using C4ModelBuilder.Analyzer;
using C4ModelBuilder.Models.Analysis;
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
    var invocationTrees = solutionAnalyzer.AnalyzeComponents(cts.Token);

    await foreach (var invocationTree in invocationTrees)
    {
        WriteInvocationTree(invocationTree);

        var plantUml = PlantUmlGenerator.Generate(invocationTree);

        var path = Path.Combine(solutionDirectory.Parent!.FullName, "output", $"{invocationTree.NodeName}.puml");
        await File.WriteAllTextAsync(path, plantUml);
        Console.WriteLine(path);
    }
}

static void WriteInvocationTree(InvocationTree node, int depth = 0)
{
    WriteLine(depth, node.NodeName);

    if (node.Invocations.Count == 0)
    {
        WriteLine(depth + 1, "<Empty>");
        return;
    }

    foreach (var child in node.Invocations)
    {
        WriteInvocationTree(child, depth + 1);
    }
}

static void WriteLine(int depth, string text) => Console.WriteLine($"{new string(' ', depth * 3)}\u2514\u2500\u2500{text}");

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
