using System.CommandLine;
using C4ModelBuilder.Cli.Models;

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
            Description = "The path to solution for analyze."
        }.AcceptLegalFilePathsOnly();

        var maxDepthOption = new Option<int>(aliases: ["--max-depth", "-d"], name: "max-depth")
        {
            Description = "Max recursion level to invoking tree analyse.",
            DefaultValueFactory = _ => 15
        };

        var rootCommand = new RootCommand("Tool for building c4 component diagram based on project marked with c4component attributes.")
        {
            solutionOption,
            outputOption,
            maxDepthOption
        };

        var parseResult = rootCommand.Parse(args);

        if (parseResult.Errors.Count == 0)
        {
            return new CliOptions(
                parseResult.GetRequiredValue(solutionOption),
                parseResult.GetRequiredValue(outputOption),
                parseResult.GetRequiredValue(maxDepthOption));
        }

        foreach (var parseError in parseResult.Errors)
        {
            Console.Error.WriteLine(parseError.Message);
        }

        return null;
    }
}
