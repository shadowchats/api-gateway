// Shadowchats — Copyright (C) 2025
// Dorovskoy Alexey Vasilievich (One290 / 0ne290) <lenya.dorovskoy@mail.ru>
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version. See the LICENSE file for details.
// For full copyright and authorship information, see the COPYRIGHT file.

using System.IO.Hashing;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace Shadowchats.ApiGateway.Presentation.YarpProxyConfigProviderFromK8S;

public class YarpProxyConfigBuilder : IYarpProxyConfigBuilder
{
    public YarpProxyConfigBuilder(IK8SEndpointSliceWatcherWorker k8SEndpointSliceWatcherWorker,
        IOptions<BaseYarpProxyConfig> baseConfigSnapshot, ILogger<YarpProxyConfigBuilder> logger)
    {
        _k8SEndpointSliceWatcherWorker = k8SEndpointSliceWatcherWorker;
        _logger = logger;
        _baseConfigSnapshot = baseConfigSnapshot.Value;
    }
    
    public YarpProxyConfig Build(CancellationTokenSource changeTokenSource)
    {
        _logger.LogInformation("[DEBUG_LABEL] YarpProxyConfigBuilder.Build() START");
        var sw = System.Diagnostics.Stopwatch.StartNew();
    
        var routes = _baseConfigSnapshot.Routes;
        _logger.LogInformation("[DEBUG_LABEL] Using {RouteCount} base routes", routes.Count);

        var clusters = _baseConfigSnapshot.Clusters.Select(cluster =>
        {
            _logger.LogInformation("[DEBUG_LABEL] Processing cluster {ClusterId}...", cluster.ClusterId);
        
            if (_k8SEndpointSliceWatcherWorker.ServiceStates.TryGetValue(cluster.ClusterId, out var serviceState))
            {
                var backends = serviceState.AllBackends;
                _logger.LogInformation("  [DEBUG_LABEL] Found {BackendCount} backends: {Backends}", 
                    backends.Count, string.Join(", ", backends.Take(5)));
            
                return cluster with
                {
                    Destinations = backends.ToDictionary(
                        GenerateId,
                        b => new DestinationConfig { Address = $"http://{b}" })
                };
            }

            _logger.LogWarning("  [DEBUG_LABEL] Service {ClusterId} NOT FOUND in ServiceStates. Available: {ServiceNames}",
                cluster.ClusterId, 
                string.Join(", ", _k8SEndpointSliceWatcherWorker.ServiceStates.Keys));
            
            return cluster;
        }).ToList();

        _logger.LogInformation("[DEBUG_LABEL] Creating YarpProxyConfig with {ClusterCount} clusters", clusters.Count);
        var config = new YarpProxyConfig
        {
            Routes = routes, 
            Clusters = clusters, 
            ChangeToken = new CancellationChangeToken(changeTokenSource.Token)
        };
    
        sw.Stop();
        _logger.LogInformation("[DEBUG_LABEL] YarpProxyConfigBuilder.Build() COMPLETE in {ElapsedMs}ms", sw.ElapsedMilliseconds);
    
        return config;
    }

    private static string GenerateId(string input) =>
        XxHash64.HashToUInt64(Encoding.UTF8.GetBytes(input)).ToString("X16");

    private readonly IK8SEndpointSliceWatcherWorker _k8SEndpointSliceWatcherWorker;

    private readonly BaseYarpProxyConfig _baseConfigSnapshot;
    
    private readonly ILogger<YarpProxyConfigBuilder> _logger;
}