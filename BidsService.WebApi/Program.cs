using Microsoft.ServiceFabric.Services.Runtime;
using Serilog;
using SullivanAuctioneers.Common.ServiceRuntime;

namespace AuctionPlatform.BidsService.WebApi;

internal class Program : ServiceEntryPoint
{
    public override string ServiceName => "BidsService.WebApi";

    /// <summary>
    /// This is the entry point of the service host process.
    /// </summary>
    private static void Main(string[] args)
    {
        new Program().Run(args);
    }

    protected override Task RunAsync(CancellationToken token)
    {
        return ServiceRuntime.RegisterServiceAsync("BidsService.WebApiType", context => new WebApi(context), cancellationToken: token);
    }

    protected override ILogger ConfigureLogger()
    {
        return Log.Logger = base.ConfigureLogger();
    }
}
