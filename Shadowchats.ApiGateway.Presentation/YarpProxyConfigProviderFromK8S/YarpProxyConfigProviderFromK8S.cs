using k8s;
using Yarp.ReverseProxy.Configuration;

namespace Shadowchats.ApiGateway.Presentation.YarpProxyConfigProviderFromK8S;

public class YarpProxyConfigProviderFromK8S : IProxyConfigProvider, IDisposable
{
    public YarpProxyConfigProviderFromK8S(ILogger<K8SEndpointSliceWatcher> logger,
        IEnumerable<string> serviceNames,
        string kubernetesNamespace)
    {
        _k8SEndpointSliceWatcher = new K8SEndpointSliceWatcher(new Kubernetes(KubernetesClientConfiguration.InClusterConfig()), logger, kubernetesNamespace, serviceNames);
        _changeTokenSource = new CancellationTokenSource();
        _builder = new YarpProxyConfigBuilder(_k8SEndpointSliceWatcher.ServiceStates);
        
        _k8SEndpointSliceWatcher.EndpointSlicesUpdated += OnEndpointSlicesUpdated;
        _current = _builder.Build(_changeTokenSource);
        
        _k8SEndpointSliceWatcher.StartWatching();
    }

    public IProxyConfig GetConfig()
    {
        lock (_locker)
            return _current;
    }

    public void Dispose()
    {
        _k8SEndpointSliceWatcher.EndpointSlicesUpdated -= OnEndpointSlicesUpdated;
        _k8SEndpointSliceWatcher.Dispose();
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

    private readonly K8SEndpointSliceWatcher _k8SEndpointSliceWatcher;
    
    private CancellationTokenSource _changeTokenSource;

    private readonly YarpProxyConfigBuilder _builder;

    private readonly Lock _locker = new();
    private YarpProxyConfig _current;
}