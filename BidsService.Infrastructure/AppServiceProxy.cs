using System.Text;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using SullivanAuctioneers.Common.ServiceRuntime;

namespace AuctionPlatform.BidsService.Infrastructure;

public static class AppServiceProxy
{
    /// <summary>
    /// Creates a client-side proxy for the Service Fabric service identified by <paramref name="serviceName"/>.
    /// Uses the specified <paramref name="ultimateId"/> to determine what service partition to connect to.
    /// </summary>
    /// <typeparam name="TService">The type of the service which must implement the IService interface.</typeparam>
    /// <param name="serviceName">The name of the service in the Service Fabric.</param>
    /// <param name="partitionKey">The ultimate key identifying the target service partition.</param>
    /// <returns>The instance of a proxy object that can be used for communicating with the service.</returns>
    public static TService Create<TService>(string serviceName, string partitionKey) where TService : IService
    {
        var builder = new ServiceUriBuilder(serviceName);
        return ServiceProxy.Create<TService>(builder.ToUri(), new ServicePartitionKey(CreateHash(partitionKey)));
    }

    /// <summary>
    /// Creates a client-side proxy for the Service Fabric service identified by <paramref name="applicationName"/> and <paramref name="serviceName"/>.
    /// Uses the specified <paramref name="ultimateId"/> to determine what service partition to connect to.
    /// </summary>
    /// <typeparam name="TService">The type of the service which must implement the IService interface.</typeparam>
    /// <param name="applicationName">The name of the Service Fabric application hosting the remoting service.</param>
    /// <param name="serviceName">The name of the service in the Service Fabric.</param>
    /// <param name="ultimateId">The ultimate key identifying the target service partition.</param>
    /// <returns>The instance of a proxy object that can be used for communicating with the service.</returns>
    public static TService Create<TService>(string applicationName, string serviceName, string partitionKey) where TService : IService
    {
        var builder = new ServiceUriBuilder(applicationName, serviceName);
        return ServiceProxy.Create<TService>(builder.ToUri(), new ServicePartitionKey(CreateHash(partitionKey)));
    }

    private static long CreateHash(string input)
    {
        var value = Encoding.UTF8.GetBytes(input.ToUpperInvariant());
        var hash = 14695981039346656037;

        for (var i = 0; i < value.Length; ++i)
        {
            hash ^= value[i];
            hash *= 1099511628211;
        }

        return (long)hash;
    }
}
