using Microsoft.Extensions.Logging;

namespace C4ModelBuilder.Cli.Models;

internal sealed record CliOptions(
    FileInfo SolutionPath,
    DirectoryInfo OutputDirectory,
    int MaxDepth,
    OutputType OutputType,
    LogLevel LogLevel,
    string? TargetProject,
    string? ExcludeMask);