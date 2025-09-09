using System.ComponentModel.DataAnnotations;
using Yarp.ReverseProxy.Configuration;

namespace Shadowchats.ApiGateway.Presentation.YarpProxyConfigProviderFromK8S;

public record BaseYarpProxyConfig
{
    [Required]
    [MinLength(1)]
    public required IReadOnlyList<RouteConfig> Routes { get; init; }
    
    [Required]
    [MinLength(1)]
    public required IReadOnlyList<ClusterConfig> Clusters { get; init; }
};