namespace Shadowchats.ApiGateway.Presentation.YarpProxyConfigProviderFromK8S;

public class K8SEndpointSliceWatcherService : BackgroundService
{
    public K8SEndpointSliceWatcherService(IK8SEndpointSliceWatcherWorker worker)
    {
        _worker = worker;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _worker.Start(stoppingToken);
    }

    private readonly IK8SEndpointSliceWatcherWorker _worker;

    
}