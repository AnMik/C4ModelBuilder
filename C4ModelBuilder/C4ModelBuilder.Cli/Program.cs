using System.Diagnostics;
using System.Text;
using C4ModelBuilder.Analyzer;
using C4ModelBuilder.Models.Analysis;
using C4ModelBuilder.PlantUmlCreator;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
var logger = loggerFactory.CreateLogger("C4ModelBuilder.Examples");
logger.LogInformation("Started.");

var sw = Stopwatch.StartNew();
using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) => CancelToken(eventArgs, cancellationTokenSource);

try
{
    await Run(cancellationTokenSource);
}
catch (TaskCanceledException)
{
    logger.LogInformation("Canceled ({elapsed\\:ss}).", sw.Elapsed);
    return;
}

logger.LogInformation("Finished ({elapsed:mm\\:ss}).", sw.Elapsed);
return;

async Task Run(CancellationTokenSource cts)
{
    var solutionFolder = GetCurrentSolutionFolderPath("C4ModelBuilder.sln");
    var solutionFilePath = Path.Combine(solutionFolder.FullName, "C4ModelBuilder.sln");
    // var solutionFilePath = "C:\\Repos\\kassa\\Afisha.Tickets.All.sln";
    var artifactsFolder = Path.Combine(solutionFolder.Parent!.FullName, "output");

    using var workspace = MSBuildWorkspace.Create();
    var solution = await workspace.OpenSolutionAsync(solutionFilePath, cancellationToken: cts.Token);

    var solutionAnalyzer = await SolutionAnalyzer.Create(logger, solution, maxDepth: 15, ct: cts.Token);
    var invocationTrees = solutionAnalyzer.AnalyzeComponents(cts.Token);

    await foreach (var invocationTree in invocationTrees)
    {
        logger.LogDebug("{tree}", FormatInvocationTree(invocationTree));

        var plantUml = PlantUmlGenerator.Generate(invocationTree);

        var path = Path.Combine(artifactsFolder, $"{invocationTree.NodeName}.puml");
        await File.WriteAllTextAsync(path, plantUml);
        logger.LogInformation("{path}.", path);
    }
}

static string FormatInvocationTree(InvocationTree node, int depth = 0)
{
    var builder = new StringBuilder();
    builder.AppendLine($"{new string(' ', depth * 3)}\u2514\u2500\u2500{node.NodeName}");

    if (node.Invocations.Count == 0)
    {
        builder.AppendLine($"{new string(' ', (depth + 1) * 3)}\u2514\u2500\u2500<Empty>");
        return builder.ToString();
    }

    foreach (var child in node.Invocations)
    {
        builder.Append(FormatInvocationTree(child, depth + 1));
    }

    return builder.ToString();
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

static DirectoryInfo GetCurrentSolutionFolderPath(string solutionFileName)
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);

    while (directory != null)
    {
        if (File.Exists(Path.Combine(directory.FullName, solutionFileName)))
        {
            return directory;
        }

        directory = directory.Parent;
    }

    throw new InvalidOperationException($"Каталог решения {solutionFileName} не найден.");
}
