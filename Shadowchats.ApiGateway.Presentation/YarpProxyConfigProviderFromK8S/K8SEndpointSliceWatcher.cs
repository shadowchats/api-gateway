using System.Collections.Concurrent;
using k8s;
using k8s.Models;

namespace Shadowchats.ApiGateway.Presentation.YarpProxyConfigProviderFromK8S;

public class K8SEndpointSliceWatcher
{
    public K8SEndpointSliceWatcher(IKubernetes apiClient, ILogger<K8SEndpointSliceWatcher> logger, string @namespace, IEnumerable<string> serviceNames)
    {
        _apiClient = apiClient;
        _logger = logger;
        _namespace = @namespace;

        foreach (var serviceName in serviceNames)
            ServiceStates[serviceName] = new K8SServiceState(serviceName);
    }

    public void StartWatching()
    {
        foreach (var serviceName in ServiceStates.Keys)
            _ = Task.Run(() => WatchWithRetry(serviceName, _cancellationTokenSource.Token));
    }

    private async Task WatchWithRetry(string serviceName, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Watch(serviceName, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Watch failed for {ServiceName}, retrying in 5s", serviceName);
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
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
                namespaceParameter: _namespace,
                plural: "endpointslices",
                watch: true,
                labelSelector: $"kubernetes.io/service-name={serviceName}",
                cancellationToken: cancellationToken
            );

        using var endpointSliceWatcher = endpointSliceList.Watch<V1EndpointSlice, V1EndpointSliceList>(
            onEvent: (watchEventType, endpointSlice) =>
            {
                if (endpointSlice?.Metadata?.Name == null) return;

                var state = ServiceStates[serviceName];

                switch (watchEventType)
                {
                    case WatchEventType.Added:
                    case WatchEventType.Modified:
                        state.EndpointSliceStates[endpointSlice.Metadata.Name] =
                            new K8SEndpointSliceState(ExtractBackendsFromEndpointSlice(endpointSlice));
                        break;
                    case WatchEventType.Deleted:
                        state.EndpointSliceStates.TryRemove(endpointSlice.Metadata.Name, out _);
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

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _apiClient.Dispose();
    }

    public ConcurrentDictionary<string, K8SServiceState> ServiceStates { get; } = new();
    public event Action? EndpointSlicesUpdated;
    
    private readonly IKubernetes _apiClient;
    private readonly ILogger<K8SEndpointSliceWatcher> _logger;
    private readonly string _namespace;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
}