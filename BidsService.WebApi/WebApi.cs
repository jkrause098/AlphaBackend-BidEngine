using System.Fabric;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using SullivanAuctioneers.Common.DependencyInjection.ServiceFabric.KeyVault;
using Serilog;
using Microsoft.Extensions.Logging;

namespace AuctionPlatform.BidsService.WebApi;

/// <summary>
/// An instance of this class is created for each service instance by the Service Fabric runtime.
/// </summary>
internal sealed class WebApi : StatelessService
{
    public WebApi(StatelessServiceContext context)
        : base(context)
    { }

    /// <summary>
    /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
    /// </summary>
    /// <returns>A collection of listeners.</returns>
    protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
    {
        return new ServiceInstanceListener[]
        {
            new ServiceInstanceListener(serviceContext =>
                new KestrelCommunicationListener(serviceContext, "ServiceEndpoint", (url, listener) =>
                {
                    return new WebHostBuilder()
                                .UseKestrel(opt =>
                                {
                                    var port = serviceContext.CodePackageActivationContext.GetEndpoint("ServiceEndpoint").Port;
                                    opt.Listen(IPAddress.IPv6Any, port, listenOptions =>
                                    {
                                        //listenOptions.UseHttps(GetCertificateFromStore());
                                    });
                                })
                                .ConfigureAppConfiguration((ctx, builder) =>
                                {
                                    builder.AddEnvironmentVariables();
                                    builder.Sources.Add(new KeyVaultConfigurationSource(ctx.HostingEnvironment.EnvironmentName, msg => Log.Information(msg)));
                                })
                                .ConfigureServices(services => services.AddSingleton(serviceContext))
                                .ConfigureLogging(logging =>
                                {
                                    logging.ClearProviders();
                                    logging.AddSerilog(Log.Logger);
                                })
                                .UseContentRoot(Directory.GetCurrentDirectory())
                                .UseStartup<Startup>()
                                .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.None)
                                .UseUrls(url)
                                .Build();
                }))
        };
    }

    /// <summary>
    /// This is the main entry point for your service instance.
    /// </summary>
    /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
    protected override Task RunAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
