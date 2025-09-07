using System.IO.Hashing;
using System.Text;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace Shadowchats.ApiGateway.Presentation.YarpProxyConfigProviderFromK8S;

public class YarpProxyConfigBuilder(
    IK8SEndpointSliceWatcherWorker k8SEndpointSliceWatcherWorker,
    YarpProxyConfig baseConfigSnapshot)
    : IYarpProxyConfigBuilder
{
    public YarpProxyConfig Build(CancellationTokenSource changeTokenSource)
    {
        var routes = baseConfigSnapshot.Routes;

        var clusters = baseConfigSnapshot.Clusters.Select(cluster =>
        {
            if (k8SEndpointSliceWatcherWorker.ServiceStates.TryGetValue(cluster.ClusterId, out var serviceState))
            {
                return cluster with
                {
                    Destinations = serviceState.AllBackends.ToDictionary(
                        b => GenerateId(b),
                        b => new DestinationConfig { Address = $"http://{b}" })
                };
            }

            return cluster;
        }).ToList();

        return new YarpProxyConfig(routes, clusters, new CancellationChangeToken(changeTokenSource.Token));
    }

    private static string GenerateId(string input) =>
        XxHash64.HashToUInt64(Encoding.UTF8.GetBytes(input)).ToString("X16");
}