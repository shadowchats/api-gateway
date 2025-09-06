using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace Shadowchats.ApiGateway.Presentation.YarpProxyConfigProviderFromK8S;

public record YarpProxyConfig(IReadOnlyList<RouteConfig> Routes, IReadOnlyList<ClusterConfig> Clusters, IChangeToken ChangeToken) : IProxyConfig;