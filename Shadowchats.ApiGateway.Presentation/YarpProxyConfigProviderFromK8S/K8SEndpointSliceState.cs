namespace Shadowchats.ApiGateway.Presentation.YarpProxyConfigProviderFromK8S;

public record K8SEndpointSliceState
{
    public required IReadOnlyList<string> Backends { get; init; }
}