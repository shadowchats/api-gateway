using System.Collections.Concurrent;

namespace Shadowchats.ApiGateway.Presentation.YarpProxyConfigProviderFromK8S;

public record K8SServiceState
{
    public K8SServiceState()
    {
        EndpointSliceStates = new ConcurrentDictionary<string, K8SEndpointSliceState>();
    }
    
    public ConcurrentDictionary<string, K8SEndpointSliceState> EndpointSliceStates { get; }
        
    public IReadOnlyList<string> AllBackends =>
        EndpointSliceStates.Values
            .SelectMany(s => s.Backends)
            .Distinct()
            .ToList();
}