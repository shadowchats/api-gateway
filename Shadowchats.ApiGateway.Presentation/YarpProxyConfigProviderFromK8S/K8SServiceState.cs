using System.Collections.Concurrent;

namespace Shadowchats.ApiGateway.Presentation.YarpProxyConfigProviderFromK8S;

public record K8SServiceState(string Name)
{
    public ConcurrentDictionary<string, K8SEndpointSliceState> EndpointSliceStates { get; } = new();
        
    public IReadOnlyList<string> AllBackends =>
        EndpointSliceStates.Values
            .SelectMany(s => s.Backends)
            .Distinct()
            .ToList();
}