using System.CommandLine;
using C4ModelBuilder.Cli.Models;
using Microsoft.Extensions.Logging;

namespace C4ModelBuilder.Cli.Infrastructure;

internal static class CliArgumentsParser
{
    public static CliOptions? Parse(string[] args)
    {
        var solutionOption = new Option<FileInfo>(aliases: ["--solution", "-s"], name: "solution")
        {
            Required = true,
            Description = "The path to solution for analyze.",
        }.AcceptLegalFilePathsOnly();

        solutionOption.Validators.Add(
            optionResult =>
            {
                var fileInfo = optionResult.GetValue(solutionOption);

                if (fileInfo?.Exists != true)
                {
                    optionResult.AddError("Solution file not exists.");
                }
                else if (fileInfo.Extension != ".sln")
                {
                    optionResult.AddError("Solution file extension is not sln.");
                }
            });

        var outputOption = new Option<DirectoryInfo>(aliases: ["--output", "-o"], name: "output")
        {
            Required = true,
            Description = "The directory path to save generated diagrams."
        }.AcceptLegalFilePathsOnly();

        var maxDepthOption = new Option<int>(aliases: ["--max-depth", "-d"], name: "max-depth")
        {
            Description = "Max recursion level to invoking tree analyse.",
            DefaultValueFactory = _ => 15
        };

        var outputTypeOption = new Option<OutputType>(aliases: ["--output-type", "-t"], name: "output-type")
        {
            Description = "The type of generated output file.",
            DefaultValueFactory = _ => OutputType.Puml
        };

        var projectOption = new Option<string?>(aliases: ["--project", "-p"], name: "project")
        {
            Description = "The specific project name in the solution to analyze (analyzes all projects by default)."
        };

        var excludeMaskOption = new Option<string?>(aliases: ["--exclude", "-e"], name: "exclude")
        {
            Description = "The mask of project names to exclude from analysis (e.g. '*tests')."
        };

        var logLevelOption = new Option<LogLevel>(aliases: ["--log-level", "-l"], name: "log-level")
        {
            Description = "The minimum log level for console output.",
            DefaultValueFactory = _ => LogLevel.Warning
        };

        var rootCommand = new RootCommand("Tool for building c4 component diagram based on project marked with c4component attributes.")
        {
            solutionOption,
            outputOption,
            maxDepthOption,
            outputTypeOption,
            projectOption,
            excludeMaskOption,
            logLevelOption
        };

        var parseResult = rootCommand.Parse(args);

        if (parseResult.Errors.Count == 0)
        {
            return new CliOptions(
                parseResult.GetRequiredValue(solutionOption),
                parseResult.GetRequiredValue(outputOption),
                parseResult.GetRequiredValue(maxDepthOption),
                parseResult.GetRequiredValue(outputTypeOption),
                parseResult.GetRequiredValue(logLevelOption),
                parseResult.GetValue(projectOption),
                parseResult.GetValue(excludeMaskOption));
        }

        foreach (var parseError in parseResult.Errors)
        {
            Console.Error.WriteLine(parseError.Message);
        }

        return null;
    }
}
