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

        var k8SNamespace = builder.Configuration.GetRequiredValue<string>("Kubernetes:Namespace");
        var k8SServiceNames = builder.Configuration.GetRequiredValue<string[]>("Kubernetes:Services");
        builder.Services.AddSingleton<IProxyConfigProvider>(sp =>
            new YarpProxyConfigProviderFromK8S.YarpProxyConfigProviderFromK8S(
                sp.GetRequiredService<ILogger<K8SEndpointSliceWatcher>>(), k8SServiceNames, k8SNamespace));

        var app = builder.Build();

        app.MapReverseProxy();

        app.Run();
    }
}