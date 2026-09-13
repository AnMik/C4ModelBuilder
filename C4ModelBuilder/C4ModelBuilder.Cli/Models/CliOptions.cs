namespace C4ModelBuilder.Cli.Models;

internal sealed record CliOptions(FileInfo SolutionPath, DirectoryInfo OutputDirectory, int MaxDepth);
