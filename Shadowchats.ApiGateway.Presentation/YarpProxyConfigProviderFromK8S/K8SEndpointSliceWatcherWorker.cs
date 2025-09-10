// Shadowchats — Copyright (C) 2025
// Dorovskoy Alexey Vasilievich (One290 / 0ne290) <lenya.dorovskoy@mail.ru>
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version. See the LICENSE file for details.
// For full copyright and authorship information, see the COPYRIGHT file.

using System.Collections.Concurrent;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Options;

namespace Shadowchats.ApiGateway.Presentation.YarpProxyConfigProviderFromK8S;

public class K8SEndpointSliceWatcherWorker : IK8SEndpointSliceWatcherWorker
{
    public K8SEndpointSliceWatcherWorker(IKubernetes apiClient, ILogger<K8SEndpointSliceWatcherWorker> logger,
        IOptions<K8SConfig> config)
    {
        _serviceStates = new ConcurrentDictionary<string, K8SServiceState>();
        ServiceStates = _serviceStates.AsReadOnly();
        
        _apiClient = apiClient;
        _logger = logger;
        _config = config.Value;

        foreach (var serviceName in _config.ServiceNames)
            _serviceStates[serviceName] = new K8SServiceState();
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        var tasks = _config.ServiceNames.Select(sn => WatchWithRetry(sn, cancellationToken));
        await Task.WhenAll(tasks);
    }

    private async Task WatchWithRetry(string serviceName, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Watch(serviceName, token);
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Watch failed for {ServiceName}, retrying in 5s", serviceName);
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), token);
                }
                catch (TaskCanceledException)
                {
                    // Ignored
                }
            }
        }
    }

    private async Task Watch(string serviceName, CancellationToken cancellationToken)
    {
        using var endpointSliceList = await _apiClient.CustomObjects
            .ListNamespacedCustomObjectWithHttpMessagesAsync<V1EndpointSliceList>(
                group: "discovery.k8s.io",
                version: "v1",
                namespaceParameter: _config.Namespace,
                plural: "endpointslices",
                watch: true,
                labelSelector: $"kubernetes.io/service-name={serviceName}",
                cancellationToken: cancellationToken
            );

        using var endpointSliceWatcher = endpointSliceList.Watch<V1EndpointSlice, V1EndpointSliceList>(
            onEvent: (watchEventType, endpointSlice) =>
            {
                if (endpointSlice?.Metadata?.Name == null)
                    return;

                switch (watchEventType)
                {
                    case WatchEventType.Added:
                    case WatchEventType.Modified:
                        _serviceStates[serviceName].EndpointSliceStates[endpointSlice.Metadata.Name] =
                            new K8SEndpointSliceState { Backends = ExtractBackendsFromEndpointSlice(endpointSlice) };
                        break;
                    case WatchEventType.Deleted:
                        _serviceStates[serviceName].EndpointSliceStates.TryRemove(endpointSlice.Metadata.Name, out _);
                        break;
                    case WatchEventType.Error:
                    case WatchEventType.Bookmark:
                    default:
                        break;
                }

                EndpointSlicesUpdated?.Invoke();
            },
            onError: ex => _logger.LogError(ex, "Watch error for {ServiceName}", serviceName),
            onClosed: () => _logger.LogInformation("Watch closed for {ServiceName}", serviceName)
        );

        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // Ignored
        }
    }

    private static List<string> ExtractBackendsFromEndpointSlice(V1EndpointSlice endpointSlice)
    {
        var backendAddresses = new List<string>();
        if (endpointSlice.Endpoints == null || endpointSlice.Ports == null)
            return backendAddresses;

        foreach (var endpoint in endpointSlice.Endpoints)
        {
            if (endpoint.Conditions?.Ready != true ||
                endpoint.Conditions?.Serving == false ||
                endpoint.Conditions?.Terminating == true)
                continue;

            if (endpoint.Addresses == null)
                continue;

            foreach (var address in endpoint.Addresses)
            foreach (var port in endpointSlice.Ports)
                if (port.Port.HasValue)
                    backendAddresses.Add(address.Contains(':')
                        ? $"[{address}]:{port.Port.Value}"// IPV6
                        : $"{address}:{port.Port.Value}"// IPV4
                    );
        }

        return backendAddresses;
    }

    public IReadOnlyDictionary<string, K8SServiceState> ServiceStates { get; }
    public event Action? EndpointSlicesUpdated;
    
    private readonly IKubernetes _apiClient;
    private readonly ILogger<K8SEndpointSliceWatcherWorker> _logger;
    private readonly K8SConfig _config;
    private readonly ConcurrentDictionary<string, K8SServiceState> _serviceStates;
}