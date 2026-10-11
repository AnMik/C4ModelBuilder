using System.CommandLine;
using C4ModelBuilder.Cli.Models;
using Microsoft.Extensions.Logging;

namespace C4ModelBuilder.Cli.Infrastructure;

/// <summary>
/// Описывает командную строку инструмента: параметры, их проверки и чтение значений.
/// </summary>
internal sealed class CliArgumentsParser
{
    private readonly Option<FileInfo> _solution = CreateSolutionOption();
    private readonly Option<DirectoryInfo> _output = CreateOutputOption();
    private readonly Option<int> _maxDepth = CreateMaxDepthOption();
    private readonly Option<OutputType> _outputType = CreateOutputTypeOption();
    private readonly Option<string?> _project = CreateProjectOption();
    private readonly Option<string?> _excludeMask = CreateExcludeMaskOption();
    private readonly Option<LogLevel> _logLevel = CreateLogLevelOption();

    public RootCommand CreateRootCommand()
        => new("Tool for building c4 component diagram based on project marked with c4component attributes.")
        {
            _solution,
            _output,
            _maxDepth,
            _outputType,
            _project,
            _excludeMask,
            _logLevel
        };

    public CliOptions ReadOptions(ParseResult parseResult)
        => new(
            parseResult.GetRequiredValue(_solution),
            parseResult.GetRequiredValue(_output),
            parseResult.GetRequiredValue(_maxDepth),
            parseResult.GetRequiredValue(_outputType),
            parseResult.GetRequiredValue(_logLevel),
            parseResult.GetValue(_project),
            parseResult.GetValue(_excludeMask));

    private static Option<FileInfo> CreateSolutionOption()
    {
        var option = new Option<FileInfo>(aliases: ["--solution", "-s"], name: "solution")
        {
            Required = true,
            Description = "The path to solution for analyze.",
        }.AcceptLegalFilePathsOnly();

        option.Validators.Add(
            optionResult =>
            {
                var fileInfo = optionResult.GetValue(option);

                if (fileInfo?.Exists != true)
                {
                    optionResult.AddError("Solution file not exists.");
                }
                else if (fileInfo.Extension != ".sln")
                {
                    optionResult.AddError("Solution file extension is not sln.");
                }
            });

        return option;
    }

    private static Option<DirectoryInfo> CreateOutputOption()
        => new Option<DirectoryInfo>(aliases: ["--output", "-o"], name: "output")
        {
            Required = true,
            Description = "The directory path to save generated diagrams."
        }.AcceptLegalFilePathsOnly();

    private static Option<int> CreateMaxDepthOption()
        => new(aliases: ["--max-depth", "-d"], name: "max-depth")
        {
            Description = "Max recursion level to invoking tree analyse.",
            DefaultValueFactory = _ => 15
        };

    private static Option<OutputType> CreateOutputTypeOption()
        => new(aliases: ["--output-type", "-t"], name: "output-type")
        {
            Description = "The type of generated output file.",
            DefaultValueFactory = _ => OutputType.Puml
        };

    private static Option<string?> CreateProjectOption()
        => new(aliases: ["--project", "-p"], name: "project")
        {
            Description = "The specific project name in the solution to analyze (analyzes all projects by default)."
        };

    private static Option<string?> CreateExcludeMaskOption()
        => new(aliases: ["--exclude", "-e"], name: "exclude")
        {
            Description = "The mask of project names to exclude from analysis (e.g. '*tests')."
        };

    private static Option<LogLevel> CreateLogLevelOption()
        => new(aliases: ["--log-level", "-l"], name: "log-level")
        {
            Description = "The minimum log level for console output.",
            DefaultValueFactory = _ => LogLevel.Warning
        };
}
