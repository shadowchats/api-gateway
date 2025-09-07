using k8s;
using Microsoft.Extensions.Primitives;
using Shadowchats.ApiGateway.Presentation.Extensions;
using Shadowchats.ApiGateway.Presentation.YarpProxyConfigProviderFromK8S;
using Yarp.ReverseProxy.Configuration;

namespace Shadowchats.ApiGateway.Presentation;

public static class Program
{
    public static void Main()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddReverseProxy();

        builder.Services.AddSingleton<IKubernetes>(_ =>
            new Kubernetes(
                KubernetesClientConfiguration.InClusterConfig()
            )
        );
        builder.Services.AddSingleton<IK8SEndpointSliceWatcherWorker>(sp =>
            new K8SEndpointSliceWatcherWorker(
                sp.GetRequiredService<IKubernetes>(),
                sp.GetRequiredService<ILogger<K8SEndpointSliceWatcherWorker>>(),
                builder.Configuration.GetRequiredValue<string>("Kubernetes:Namespace"),
                builder.Configuration.GetRequiredValue<string[]>("Kubernetes:Services")
            )
        );
        builder.Services.AddSingleton<IYarpProxyConfigBuilder>(sp =>
            new YarpProxyConfigBuilder(
                sp.GetRequiredService<IK8SEndpointSliceWatcherWorker>(),
                new YarpProxyConfig(
                    builder.Configuration.GetRequired<RouteConfig[]>("ReverseProxy:Routes"),
                    builder.Configuration.GetRequired<ClusterConfig[]>("ReverseProxy:Clusters"),
                    new CancellationChangeToken(CancellationToken.None)
                )
            )
        );
        
        builder.Services.AddHostedService<K8SEndpointSliceWatcherService>();

        builder.Services.AddSingleton<IProxyConfigProvider, YarpProxyConfigProviderFromK8S.YarpProxyConfigProviderFromK8S>();

        var app = builder.Build();

        app.MapReverseProxy();

        app.Run();
    }
}