using System.Collections.Concurrent;
using System.IO.Hashing;
using System.Text;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace Shadowchats.ApiGateway.Presentation.YarpProxyConfigProviderFromK8S;

public class YarpProxyConfigBuilder
{
    public YarpProxyConfigBuilder(ConcurrentDictionary<string, K8SServiceState> k8SServiceStates)
    {
        _k8SServiceStates = k8SServiceStates;
    }

    public YarpProxyConfig Build(CancellationTokenSource changeTokenSource)
    {
        var routes = new List<RouteConfig>();
        var clusters = new List<ClusterConfig>();

        foreach (var k8SServiceState in _k8SServiceStates.Values)
        {
            var clusterId = GenerateId($"kube-{k8SServiceState.Name}");
            var backends = k8SServiceState.AllBackends;

            routes.Add(new RouteConfig
            {
                RouteId = GenerateId($"route-{k8SServiceState.Name}"),
                ClusterId = clusterId,
                Match = new RouteMatch { Path = $"/{k8SServiceState.Name}/{{**catchall}}" }
            });

            clusters.Add(new ClusterConfig
            {
                ClusterId = clusterId,
                LoadBalancingPolicy = "RoundRobin",
                Destinations = backends.ToDictionary(
                    b => GenerateId($"dest-{b}"),
                    b => new DestinationConfig { Address = $"http://{b}" })
            });
        }

        return new YarpProxyConfig(routes, clusters, new CancellationChangeToken(changeTokenSource.Token));
    }

    private static string GenerateId(string input) =>
        XxHash64.HashToUInt64(Encoding.UTF8.GetBytes(input)).ToString("X16");
    
    private readonly ConcurrentDictionary<string, K8SServiceState> _k8SServiceStates;
}