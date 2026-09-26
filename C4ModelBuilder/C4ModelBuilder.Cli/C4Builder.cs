using C4ModelBuilder.Analyzer;
using C4ModelBuilder.Cli.Infrastructure;
using C4ModelBuilder.Cli.Models;
using C4ModelBuilder.PlantUmlCreator;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace C4ModelBuilder.Cli;

internal sealed class C4Builder(ILogger<C4Builder> logger, IOptions<CliOptions> options)
{
    public async Task Run(CancellationToken ct)
    {
        using var workspace = MSBuildWorkspace.Create();
        var solution = await workspace.OpenSolutionAsync(options.Value.SolutionPath.FullName, cancellationToken: ct);

        var solutionAnalyzer = await SolutionAnalyzer.Create(
            logger,
            solution,
            options.Value.MaxDepth,
            options.Value.TargetProject,
            options.Value.ExcludeMask,
            ct);

        var invocationTrees = solutionAnalyzer.AnalyzeComponents(ct);

        await foreach (var invocationTree in invocationTrees)
        {
            logger.LogDebug("{tree}", InvocationTreeFormatter.Format(invocationTree));

            var extension = options.Value.OutputType switch
            {
                OutputType.Puml => "puml",
                OutputType.Mermaid => throw new NotImplementedException(),
                _ => "puml"
            };

            var plantUml = PlantUmlGenerator.Generate(invocationTree);

            var outputPath = Path.Combine(options.Value.OutputDirectory.FullName, $"{invocationTree.NodeName}.{extension}");
            await File.WriteAllTextAsync(outputPath, plantUml, ct);
            logger.LogInformation("Created {path}.", outputPath);
        }
    }
}
