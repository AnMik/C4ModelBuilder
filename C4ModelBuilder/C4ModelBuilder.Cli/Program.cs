using System.Diagnostics;
using C4ModelBuilder.Cli;
using C4ModelBuilder.Cli.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var options = CliArgumentsParser.Parse(args);

if (options == null)
{
    return 1;
}

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
services.AddTransient<C4Builder>();

await using var serviceProvider = services.BuildServiceProvider();

var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Started for {input}", options.SolutionPath);

var stopwatch = Stopwatch.StartNew();
using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) => CancelToken(eventArgs, cancellationTokenSource);

var c4Builder = serviceProvider.GetRequiredService<C4Builder>();

try
{
    await c4Builder.Run(options.SolutionPath, options.OutputDirectory, options.MaxDepth, cancellationTokenSource.Token);
}
catch (TaskCanceledException)
{
    logger.LogInformation("Canceled ({elapsed:ss}).", stopwatch.Elapsed);
    return 130;
}
catch (Exception)
{
    cancellationTokenSource.Cancel();
    return 1;
}

logger.LogInformation("Finished ({elapsed:mm\\:ss}).", stopwatch.Elapsed);
return 0;

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