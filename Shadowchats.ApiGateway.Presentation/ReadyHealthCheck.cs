using Microsoft.Extensions.Diagnostics.HealthChecks;
using Yarp.ReverseProxy.Configuration;

namespace Shadowchats.ApiGateway.Presentation;

public class ReadyHealthCheck : IHealthCheck
{
    public ReadyHealthCheck(IProxyConfigProvider yarpConfigProvider, ILogger<ReadyHealthCheck> logger)
    {
        _yarpConfigProvider = yarpConfigProvider;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[DEBUG_LABEL] ReadyHealthCheck: Getting config...");
        var config = _yarpConfigProvider.GetConfig();
    
        _logger.LogInformation("[DEBUG_LABEL] ReadyHealthCheck: Config has {ClusterCount} clusters", config.Clusters.Count);
    
        var allReady = config.Clusters
            .All(cluster => cluster.Destinations != null && cluster.Destinations.Any());
    
        if (!allReady)
        {
            _logger.LogWarning("[DEBUG_LABEL] ReadyHealthCheck: Not ready. Clusters without destinations:");
            foreach (var cluster in config.Clusters.Where(c => c.Destinations == null || !c.Destinations.Any()))
            {
                _logger.LogWarning("  [DEBUG_LABEL] - {ClusterId}: {DestCount} destinations",
                    cluster.ClusterId, cluster.Destinations?.Count ?? 0);
            }
        }
    
        return Task.FromResult(allReady 
            ? HealthCheckResult.Healthy() 
            : HealthCheckResult.Unhealthy("Not all clusters have destinations"));
    }

    private readonly IProxyConfigProvider _yarpConfigProvider;
    
    private readonly ILogger<ReadyHealthCheck> _logger;
}