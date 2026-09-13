using System.Diagnostics;
using C4ModelBuilder.Cli;
using C4ModelBuilder.Cli.Infrastructure;
using Microsoft.Extensions.Logging;

var options = CliArgumentsParser.Parse(args);

if (options == null)
{
    return 1;
}

using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
var logger = loggerFactory.CreateLogger("C4ModelBuilder.Cli");
logger.LogInformation("Started for {input}", options.SolutionPath);

var stopwatch = Stopwatch.StartNew();
using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) => CancelToken(eventArgs, cancellationTokenSource);

var c4Builder = new C4Builder(loggerFactory);

try
{
    await c4Builder.Run(options.SolutionPath, options.OutputDirectory, options.MaxDepth, cancellationTokenSource.Token);
}
catch (TaskCanceledException)
{
    logger.LogInformation("Canceled ({elapsed\\:ss}).", stopwatch.Elapsed);
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
