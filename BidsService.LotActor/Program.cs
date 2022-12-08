using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Actors.Runtime;
using Serilog;
using AuctionPlatform.BidsService.Common;
using AuctionPlatform.BidsService.Common.Serialization;
using SullivanAuctioneers.Common.DependencyInjection.ServiceFabric;
using SullivanAuctioneers.Common.ServiceRuntime;
using ILogger = Serilog.ILogger;

namespace AuctionPlatform.BidsService.LotActor;

internal class Program : ServiceEntryPoint
{
    public override string ServiceName => "BidsService.LotActor";

    /// <summary>
    /// This is the entry point of the service host process.
    /// </summary>
    private static void Main(string[] args)
    {
        new Program().Run(args);
    }

    protected override Task RunAsync(CancellationToken token)
    {
        return Host.CreateDefaultBuilder()
                   .ConfigureAppConfiguration((hostBuilderContext, configurationBuilder) =>
                   {
                       hostBuilderContext.HostingEnvironment.EnvironmentName =
                           Environment.GetEnvironmentVariable(GlobalConsts.AppEnvironmentVariable) ??
                           throw new InvalidOperationException($"Environment variable {GlobalConsts.AppEnvironmentVariable} not found");
                   })
                   .ConfigureServices(ConfigureServices)
                   .WithKeyVaultConfiguration(msg => Log.Information(msg))
                   .ConfigureLogging(logging =>
                   {
                       logging.ClearProviders();
                       logging.AddSerilog(Log.Logger);
                   })
                   .WithActor<LotActor>(provider =>
                   {
                       var logger = provider.GetRequiredService<ILogger>();
                       var configuration = provider.GetRequiredService<IConfiguration>();
                       var serializer = provider.GetRequiredService<ISerializer>();

                       return (context, actorType) => new ActorService(context, actorType, (service, id) => new LotActor(service, id, configuration, logger, serializer));
                   })
                   .AddServiceFabric()
                   .Build()
                   .RunAsync(token: token);
    }

    protected override ILogger ConfigureLogger()
    {
        return Log.Logger = base.ConfigureLogger();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(Logger);
        services.AddSingleton<ISerializer, JsonSerializer>();
    }
}
