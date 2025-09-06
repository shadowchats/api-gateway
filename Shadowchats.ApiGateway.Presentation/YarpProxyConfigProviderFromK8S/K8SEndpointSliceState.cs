namespace Shadowchats.ApiGateway.Presentation.YarpProxyConfigProviderFromK8S;

public record K8SEndpointSliceState(IReadOnlyList<string> Backends);