using Microsoft.Extensions.Diagnostics.HealthChecks;
using Yarp.ReverseProxy.Configuration;

namespace Shadowchats.ApiGateway.Presentation;

public class ReadyHealthCheck : IHealthCheck
{
    public ReadyHealthCheck(IProxyConfigProvider yarpConfigProvider)
    {
        _yarpConfigProvider = yarpConfigProvider;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var config = _yarpConfigProvider.GetConfig();
        
        var allReady = config.Clusters
            .All(cluster => cluster.Destinations != null && cluster.Destinations.Any());
        
        return Task.FromResult(allReady 
            ? HealthCheckResult.Healthy() 
            : HealthCheckResult.Unhealthy("Not all clusters have destinations"));
    }

    private readonly IProxyConfigProvider _yarpConfigProvider;
}