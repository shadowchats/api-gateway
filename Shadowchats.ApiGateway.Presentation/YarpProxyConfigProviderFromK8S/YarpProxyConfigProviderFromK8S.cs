using Yarp.ReverseProxy.Configuration;

namespace Shadowchats.ApiGateway.Presentation.YarpProxyConfigProviderFromK8S;

public class YarpProxyConfigProviderFromK8S : IProxyConfigProvider, IDisposable
{
    public YarpProxyConfigProviderFromK8S(IK8SEndpointSliceWatcherWorker k8SEndpointSliceWatcherWorker, IYarpProxyConfigBuilder builder)
    {
        _k8SEndpointSliceWatcherWorker = k8SEndpointSliceWatcherWorker;
        _builder = builder;
        
        _changeTokenSource = new CancellationTokenSource();

        _k8SEndpointSliceWatcherWorker.EndpointSlicesUpdated += OnEndpointSlicesUpdated;
        _current = _builder.Build(_changeTokenSource);
    }

    public IProxyConfig GetConfig()
    {
        lock (_locker)
            return _current;
    }

    public void Dispose()
    {
        _k8SEndpointSliceWatcherWorker.EndpointSlicesUpdated -= OnEndpointSlicesUpdated;
        _changeTokenSource.Cancel();
        _changeTokenSource.Dispose();
    }
    
    private void OnEndpointSlicesUpdated()
    {
        lock (_locker)
        {
            _changeTokenSource.Cancel();
            var oldChangeTokenSource = _changeTokenSource;
            _changeTokenSource = new CancellationTokenSource();
            _current = _builder.Build(_changeTokenSource);
            oldChangeTokenSource.Dispose();
        }
    }

    private readonly IK8SEndpointSliceWatcherWorker _k8SEndpointSliceWatcherWorker;
    
    private readonly IYarpProxyConfigBuilder _builder;
    
    private CancellationTokenSource _changeTokenSource;

    private readonly Lock _locker = new();
    private YarpProxyConfig _current;
}