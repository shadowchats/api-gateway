using System.Collections.Concurrent;
using System.IO.Hashing;
using System.Text;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace Shadowchats.ApiGateway.Presentation;

public sealed class KubernetesConfigProvider : IProxyConfigProvider, IDisposable
{
    private sealed record ProxyConfig(
        IReadOnlyList<RouteConfig> Routes,
        IReadOnlyList<ClusterConfig> Clusters,
        IChangeToken ChangeToken) : IProxyConfig;

    private sealed record SliceState(IReadOnlyList<string> Backends);

    private sealed record ServiceState(string Name, ConcurrentDictionary<string, SliceState> Slices)
    {
        public readonly IReadOnlyList<string> AllBackends =
            Slices.Values
                .SelectMany(s => s.Backends)
                .Distinct()
                .ToList();
    }

    private readonly Kubernetes _client;
    private readonly ILogger<KubernetesConfigProvider> _logger;
    private readonly string _namespace;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<string, ServiceState> _services = new();
    private readonly object _configLock = new();

    private ProxyConfig _currentConfig;
    private CancellationTokenSource _changeTokenSource = new();

    public KubernetesConfigProvider(
        ILogger<KubernetesConfigProvider> logger,
        IEnumerable<string> serviceNames,
        string @namespace = "default")
    {
        _logger = logger;
        _namespace = @namespace;

        var config = KubernetesClientConfiguration.InClusterConfig();
        _client = new Kubernetes(config);

        foreach (var serviceName in serviceNames)
        {
            var state = new ServiceState(serviceName, new ConcurrentDictionary<string, SliceState>());
            _services[serviceName] = state;

            _ = Task.Run(() => WatchEndpointSlicesWithRetry(serviceName, _cts.Token));
        }

        _currentConfig = BuildConfig();
    }

    private async Task WatchEndpointSlicesWithRetry(string serviceName, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await WatchEndpointSlices(serviceName, ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Watch failed for {ServiceName}, retrying in 5s", serviceName);
                try { await Task.Delay(TimeSpan.FromSeconds(5), ct); } catch { }
            }
        }
    }

    private async Task WatchEndpointSlices(string serviceName, CancellationToken ct)
    {
        using var resp = await _client.CustomObjects
            .ListNamespacedCustomObjectWithHttpMessagesAsync<V1EndpointSliceList>(
                group: "discovery.k8s.io",
                version: "v1",
                namespaceParameter: _namespace,
                plural: "endpointslices",
                watch: true,
                labelSelector: $"kubernetes.io/service-name={serviceName}",
                cancellationToken: ct);

        using var watcher = resp.Watch<V1EndpointSlice, V1EndpointSliceList>(
            onEvent: (type, slice) =>
            {
                if (slice?.Metadata?.Name == null) return;

                switch (type)
                {
                    case WatchEventType.Added:
                    case WatchEventType.Modified:
                        _services[serviceName].Slices[slice.Metadata.Name] = new SliceState(ExtractBackends(slice));
                        break;
                    case WatchEventType.Deleted:
                        _services[serviceName].Slices.TryRemove(slice.Metadata.Name, out _);
                        break;
                }

                UpdateConfig();
            },
            onError: ex => _logger.LogError(ex, "Watch error for {ServiceName}", serviceName),
            onClosed: () => _logger.LogInformation("Watch closed for {ServiceName}", serviceName));

        try
        {
            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (TaskCanceledException) { }
    }

    private static List<string> ExtractBackends(V1EndpointSlice slice)
    {
        var result = new List<string>();
        if (slice.Endpoints == null || slice.Ports == null) return result;

        foreach (var endpoint in slice.Endpoints)
        {
            if (endpoint.Conditions?.Ready != true ||
                endpoint.Conditions?.Serving == false ||
                endpoint.Conditions?.Terminating == true)
                continue;

            if (endpoint.Addresses == null) continue;

            foreach (var address in endpoint.Addresses)
            {
                foreach (var port in slice.Ports)
                {
                    if (!port.Port.HasValue) continue;

                    var hostname = address.Contains(':')
                        ? $"[{address}]:{port.Port.Value}"// IPV6
                        : $"{address}:{port.Port.Value}"; // IPV4
                    result.Add(hostname);
                }
            }
        }

        return result;
    }

    private void UpdateConfig()
    {
        lock (_configLock)
        {
            var old = _changeTokenSource;
            _changeTokenSource = new CancellationTokenSource();
            _currentConfig = BuildConfig(_changeTokenSource);

            old.Cancel();
            old.Dispose();
        }
    }

    private ProxyConfig BuildConfig(CancellationTokenSource? cts = null)
    {
        var routes = new List<RouteConfig>();
        var clusters = new List<ClusterConfig>();

        foreach (var serviceState in _services.Values)
        {
            var clusterId = GenerateId($"kube-{serviceState.Name}");
            var backends = serviceState.AllBackends;

            routes.Add(new RouteConfig
            {
                RouteId = GenerateId($"route-{serviceState.Name}"),
                ClusterId = clusterId,
                Match = new RouteMatch { Path = $"/{serviceState.Name}/{{**catchall}}" }
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

        var token = cts != null
            ? new CancellationChangeToken(cts.Token)
            : new CancellationChangeToken(CancellationToken.None);

        return new ProxyConfig(routes, clusters, token);
    }

    private static string GenerateId(string input) =>
        XxHash64.HashToUInt64(Encoding.UTF8.GetBytes(input)).ToString("X16");

    public IProxyConfig GetConfig()
    {
        lock (_configLock)
        {
            return _currentConfig;
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _changeTokenSource.Dispose();
        _client.Dispose();
        _cts.Dispose();
    }
}
