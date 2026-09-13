using C4ModelBuilder.Analyzer;
using C4ModelBuilder.Cli.Infrastructure;
using C4ModelBuilder.PlantUmlCreator;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;

namespace C4ModelBuilder.Cli;

internal sealed class C4Builder(ILogger<C4Builder> logger)
{
    public async Task Run(FileInfo solutionFile, DirectoryInfo outputDirectory, int maxDepth, CancellationToken ct)
    {
        using var workspace = MSBuildWorkspace.Create();
        var solution = await workspace.OpenSolutionAsync(solutionFile.FullName, cancellationToken: ct);

        var solutionAnalyzer = await SolutionAnalyzer.Create(logger, solution, maxDepth, ct);
        var invocationTrees = solutionAnalyzer.AnalyzeComponents(ct);

        await foreach (var invocationTree in invocationTrees)
        {
            logger.LogDebug("{tree}", InvocationTreeFormatter.Format(invocationTree));

            var plantUml = PlantUmlGenerator.Generate(invocationTree);

            var outputPath = Path.Combine(outputDirectory.FullName, $"{invocationTree.NodeName}.puml");
            await File.WriteAllTextAsync(outputPath, plantUml, ct);
            logger.LogInformation("Created {path}.", outputPath);
        }
    }
}
