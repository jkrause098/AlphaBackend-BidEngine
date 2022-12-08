using Microsoft.Azure.SignalR.Management;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using Serilog;
using AuctionPlatform.BidsService.Common;
using AuctionPlatform.BidsService.Infrastructure;
using SullivanAuctioneers.Common.DependencyInjection.ServiceFabric;
using SullivanAuctioneers.Common.ServiceRuntime;
using AuctionPlatform.Contract.Interfaces;
using AuctionPlatform.Contract.Models.Authentication;
using AuctionPlatform.ResourceAccess.EntityFramework;
using ILogger = Serilog.ILogger;

namespace AuctionPlatform.BidsService.AuctionController;

internal class Program : ServiceEntryPoint
{
    public override string ServiceName => "BidsService.AuctionController";

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
                   .ConfigureServices((builder, services) =>
                   {
                       ConfigureServices(builder.Configuration, services, builder);
                   })
                   .WithKeyVaultConfiguration(msg => Log.Information(msg))
                   .ConfigureLogging(logging =>
                   {
                       logging.ClearProviders();
                       logging.AddSerilog(Log.Logger);
                   })
                   .WithStatefulService("BidsService.AuctionControllerType", (provider, context) => new AuctionController(context, provider))
                   .AddServiceFabric()
                   .Build()
                   .RunAsync(token: token);
    }

    protected override ILogger ConfigureLogger()
    {
        return Log.Logger = base.ConfigureLogger();
    }

    private void ConfigureServices(IConfiguration configuration, IServiceCollection services, HostBuilderContext context)
    {
        // Register the global logging component.
        services.AddSingleton(Logger);

        // Register data access layer and its supporting infrastructure.
        services.AddScoped<IUserSession, UserSession>();
        services.AddDbContextFactory<SaDbContext>(options => options.UseSqlServer(configuration[GlobalConsts.SettingNames.SqlConnectionString],
                                                  providerOptions => providerOptions.EnableRetryOnFailure()));
        // Register custom transformation components.
        services.AddDomainTransforms();

        // Configure the SignalR hub communication component.
        services.AddSingleton(new ServiceManagerBuilder().WithOptions(options =>
        {
            var environmentName = context.HostingEnvironment.EnvironmentName;
            var isLocal = environmentName == GlobalConsts.CustomDevelopmentEnvironmentName;

            options.ConnectionString = configuration[isLocal ? GlobalConsts.SettingNames.SignalRConnectionStringLocal : GlobalConsts.SettingNames.SignalRConnectionString];
            options.ServiceTransportType = ServiceTransportType.Transient;
        })
        .WithLoggerFactory(LoggerFactory.Create(builder => builder.AddSerilog(Log.Logger)))
        .WithNewtonsoftJson(options =>
        {
            options.PayloadSerializerSettings.ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };
        })
        .BuildServiceManager());

        // Configure the Service Bus messaging client.
        services.AddAzureClients(builder =>
        {
            builder.AddServiceBusClient(configuration[GlobalConsts.SettingNames.ServiceBusSenderConnectionString]);
        });
    }
}
