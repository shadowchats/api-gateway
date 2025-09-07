namespace Shadowchats.ApiGateway.Presentation.YarpProxyConfigProviderFromK8S;

public interface IK8SEndpointSliceWatcherWorker
{
    IReadOnlyDictionary<string, K8SServiceState> ServiceStates { get; }
    event Action? EndpointSlicesUpdated;
    Task Start(CancellationToken cancellationToken);
}