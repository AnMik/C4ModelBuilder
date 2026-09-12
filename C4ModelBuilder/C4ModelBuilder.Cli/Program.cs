using System.Diagnostics;
using System.Text;
using C4ModelBuilder.Analyzer;
using C4ModelBuilder.Models.Analysis;
using C4ModelBuilder.PlantUmlCreator;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

var options = ParseCommandLine(args);

if (options.InputPath == null)
{
    PrintUsage();
    return 1;
}

var inputPath = Path.GetFullPath(options.InputPath);
var outputDirectory = Path.GetFullPath(options.OutputDirectory ?? "output");

using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
var logger = loggerFactory.CreateLogger("C4ModelBuilder.Cli");
logger.LogInformation("Started: {input}", inputPath);

var stopwatch = Stopwatch.StartNew();
using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) => CancelToken(eventArgs, cancellationTokenSource);

try
{
    Directory.CreateDirectory(outputDirectory);

    await Run(inputPath, outputDirectory, options.MaxDepth, logger, cancellationTokenSource);
}
catch (TaskCanceledException)
{
    logger.LogInformation("Canceled ({elapsed\\:ss}).", stopwatch.Elapsed);
    return 130;
}

logger.LogInformation("Finished ({elapsed:mm\\:ss}).", stopwatch.Elapsed);
return 0;

async Task Run(string inputPath, string outputDirectory, int maxDepth, ILogger logger, CancellationTokenSource cts)
{
    var solution = await OpenInputAsync(inputPath, cts.Token);

    var solutionAnalyzer = await SolutionAnalyzer.Create(logger, solution, maxDepth, cts.Token);
    var invocationTrees = solutionAnalyzer.AnalyzeComponents(cts.Token);

    await foreach (var invocationTree in invocationTrees)
    {
        logger.LogDebug("{tree}", FormatInvocationTree(invocationTree));

        var plantUml = PlantUmlGenerator.Generate(invocationTree);
        var outputPath = Path.Combine(outputDirectory, $"{invocationTree.NodeName}.puml");
        await File.WriteAllTextAsync(outputPath, plantUml, cts.Token);
        logger.LogInformation("Created {path}.", outputPath);
    }
}

async Task<Solution> OpenInputAsync(string inputPath, CancellationToken ct)
{
    if (!File.Exists(inputPath))
    {
        throw new FileNotFoundException($"Не найден входной файл {inputPath}.", inputPath);
    }

    var extension = Path.GetExtension(inputPath);
    if (extension.Equals(".sln", StringComparison.OrdinalIgnoreCase))
    {
        using var workspace = Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace.Create();
        return await workspace.OpenSolutionAsync(inputPath, cancellationToken: ct);
    }

    if (extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase))
    {
        using var workspace = Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(inputPath, cancellationToken: ct);
        return project.Solution;
    }

    throw new InvalidOperationException("Входной файл должен иметь расширение .sln или .csproj.");
}

CliOptions ParseCommandLine(string[] arguments)
{
    if (arguments.Any(argument => argument is "-h" or "--help"))
    {
        return new CliOptions(null, null, 15);
    }

    string? inputPath = null;
    string? outputDirectory = null;
    var maxDepth = 15;

    for (var index = 0; index < arguments.Length; index++)
    {
        var argument = arguments[index];

        switch (argument)
        {
            case "--solution":
            case "-s":
                inputPath = ReadValue(arguments, ++index, argument);
                break;
            case "--output":
            case "-o":
                outputDirectory = ReadValue(arguments, ++index, argument);
                break;
            case "--max-depth":
            case "-d":
                maxDepth = ParseMaxDepth(ReadValue(arguments, ++index, argument));
                break;
            default:
                if (inputPath == null && !argument.StartsWith("-", StringComparison.Ordinal))
                {
                    inputPath = argument;
                    break;
                }

                throw new ArgumentException($"Неизвестный аргумент {argument}.");
        }
    }

    return new CliOptions(inputPath, outputDirectory ?? "output", maxDepth);
}

string ReadValue(string[] arguments, int valueIndex, string optionName)
{
    if (valueIndex >= arguments.Length || arguments[valueIndex].StartsWith("-", StringComparison.Ordinal))
    {
        throw new ArgumentException($"Для аргумента {optionName} не задано значение.");
    }

    return arguments[valueIndex];
}

int ParseMaxDepth(string value)
    => int.TryParse(value, out var maxDepth) && maxDepth >= 0
        ? maxDepth
        : throw new ArgumentException("--max-depth должен быть неотрицательным целым числом.");

void PrintUsage()
{
    Console.WriteLine(
        "Использование: C4ModelBuilder.Cli <путь-к-.sln-или-.csproj> [--solution <path>] [--output <directory>] [--max-depth <number>]");
}

string FormatInvocationTree(InvocationTree node, int depth = 0)
{
    var builder = new StringBuilder();
    builder.AppendLine($"{new string(' ', depth * 3)}└──{node.NodeName}");

    if (node.Invocations.Count == 0)
    {
        builder.AppendLine($"{new string(' ', (depth + 1) * 3)}└──<Empty>");
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

sealed record CliOptions(string? InputPath, string? OutputDirectory, int MaxDepth);