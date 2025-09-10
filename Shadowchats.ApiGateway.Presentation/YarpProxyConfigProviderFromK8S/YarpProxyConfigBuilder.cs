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
        IOptions<BaseYarpProxyConfig> baseConfigSnapshot)
    {
        _k8SEndpointSliceWatcherWorker = k8SEndpointSliceWatcherWorker;
        _baseConfigSnapshot = baseConfigSnapshot.Value;
    }
    
    public YarpProxyConfig Build(CancellationTokenSource changeTokenSource)
    {
        var routes = _baseConfigSnapshot.Routes;

        var clusters = _baseConfigSnapshot.Clusters.Select(cluster =>
        {
            if (_k8SEndpointSliceWatcherWorker.ServiceStates.TryGetValue(cluster.ClusterId, out var serviceState))
            {
                return cluster with
                {
                    Destinations = serviceState.AllBackends.ToDictionary(
                        GenerateId,
                        b => new DestinationConfig { Address = $"http://{b}" })
                };
            }

            return cluster;
        }).ToList();

        return new YarpProxyConfig
        {
            Routes = routes, Clusters = clusters, ChangeToken = new CancellationChangeToken(changeTokenSource.Token)
        };
    }

    private static string GenerateId(string input) =>
        XxHash64.HashToUInt64(Encoding.UTF8.GetBytes(input)).ToString("X16");

    private readonly IK8SEndpointSliceWatcherWorker _k8SEndpointSliceWatcherWorker;

    private readonly BaseYarpProxyConfig _baseConfigSnapshot;
}