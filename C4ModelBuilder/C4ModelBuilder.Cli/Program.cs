using System.Diagnostics;
using C4ModelBuilder.Cli;
using C4ModelBuilder.Cli.Infrastructure;
using C4ModelBuilder.Cli.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var sw = Stopwatch.StartNew();
using var outerCts = new CancellationTokenSource();

var options = CliArgumentsParser.Parse(args);

if (options == null)
{
    return 1;
}

await using var serviceProvider = BuildServiceProvider(options, outerCts);

var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Started for {input}", options.SolutionPath);

try
{
    var c4Builder = serviceProvider.GetRequiredService<C4Builder>();
    await c4Builder.Run(outerCts.Token);
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

ServiceProvider BuildServiceProvider(CliOptions cliOptions, CancellationTokenSource cts)
{
    Console.CancelKeyPress += (_, eventArgs) => CancelToken(eventArgs, cts);

    var services = new ServiceCollection();
    services.AddServices(cliOptions);
    return services.BuildServiceProvider();
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
