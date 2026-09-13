using C4ModelBuilder.Cli.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace C4ModelBuilder.Cli.Infrastructure;

internal static class IServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection serviceCollection, CliOptions cliOptions)
    {
        serviceCollection.AddSingleton(Options.Create(cliOptions));
        serviceCollection.AddLogging(builder => builder.AddConsole().SetMinimumLevel(cliOptions.LogLevel));
        serviceCollection.AddTransient<C4Builder>();
        return serviceCollection;
    }
}
