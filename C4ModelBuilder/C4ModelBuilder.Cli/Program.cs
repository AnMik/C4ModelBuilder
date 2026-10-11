// ReSharper disable AccessToDisposedClosure
using System.CommandLine.Invocation;
using System.Diagnostics;
using C4ModelBuilder.Cli;
using C4ModelBuilder.Cli.Infrastructure;
using C4ModelBuilder.Cli.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) => CancelToken(eventArgs, cts);

return await Run(args, cts.Token);

async Task<int> Run(string[] args, CancellationToken ct)
{
    var parser = new CliArgumentsParser();
    var command = parser.CreateRootCommand();
    command.SetAction((parseResult, _) => BuildDiagrams(parser.ReadOptions(parseResult), ct));

    var commandLine = command.Parse(args);

    if (commandLine.Errors.Count > 0 && commandLine.Action is not ParseErrorAction)
    {
        // Выбранное действие подавляет ошибки разбора: так библиотека обходит отказ «--version вместе с другими параметрами».
        return new ParseErrorAction().Invoke(commandLine);
    }

    return await commandLine.InvokeAsync();
}

async Task<int> BuildDiagrams(CliOptions cliOptions, CancellationToken ct)
{
    var sw = Stopwatch.StartNew();

    await using var sp = new ServiceCollection().AddServices(cliOptions).BuildServiceProvider();
    var logger = sp.GetRequiredService<ILogger<Program>>();
    var c4Builder = sp.GetRequiredService<C4Builder>();

    logger.LogInformation("Started for {input}", cliOptions.SolutionPath);

    try
    {
        await c4Builder.Run(ct);
    }
    catch (TaskCanceledException)
    {
        logger.LogInformation("Canceled ({elapsed:ss}).", sw.Elapsed);
        return 130;
    }
    catch (Exception e)
    {
        logger.LogError(e, "Exception ({elapsed:ss}).", sw.Elapsed);
        return 1;
    }

    logger.LogInformation("Finished ({elapsed:mm\\:ss}).", sw.Elapsed);
    return 0;
}

void CancelToken(ConsoleCancelEventArgs args, CancellationTokenSource cancellationTokenSource)
{
    args.Cancel = true;

    try
    {
        cancellationTokenSource.Cancel();
    }
    catch (ObjectDisposedException)
    {
        // ignore
    }
}
