namespace Shadowchats.ApiGateway.Presentation.YarpProxyConfigProviderFromK8S;

public class K8SEndpointSliceWatcherService(IK8SEndpointSliceWatcherWorker worker) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return worker.Start(stoppingToken);
    }
}