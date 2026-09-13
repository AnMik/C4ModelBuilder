using C4ModelBuilder.Analyzer;
using C4ModelBuilder.Cli.Infrastructure;
using C4ModelBuilder.PlantUmlCreator;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;

namespace C4ModelBuilder.Cli;

internal sealed class C4Builder(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger(nameof(C4Builder));

    public async Task Run(FileInfo solutionFile, DirectoryInfo outputDirectory, int maxDepth, CancellationToken ct)
    {
        using var workspace = MSBuildWorkspace.Create();
        var solution = await workspace.OpenSolutionAsync(solutionFile.FullName, cancellationToken: ct);

        var solutionAnalyzer = await SolutionAnalyzer.Create(_logger, solution, maxDepth, ct);
        var invocationTrees = solutionAnalyzer.AnalyzeComponents(ct);

        await foreach (var invocationTree in invocationTrees)
        {
            _logger.LogDebug("{tree}", InvocationTreeFormatter.Format(invocationTree));

            var plantUml = PlantUmlGenerator.Generate(invocationTree);

            var outputPath = Path.Combine(outputDirectory.FullName, $"{invocationTree.NodeName}.puml");
            await File.WriteAllTextAsync(outputPath, plantUml, ct);
            _logger.LogInformation("Created {path}.", outputPath);
        }
    }
}
