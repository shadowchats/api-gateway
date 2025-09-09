using System.IdentityModel.Tokens.Jwt;
using k8s;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Shadowchats.ApiGateway.Presentation.YarpProxyConfigProviderFromK8S;
using Yarp.ReverseProxy.Configuration;

namespace Shadowchats.ApiGateway.Presentation;

public static class Program
{
    public static void Main()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddReverseProxy();

        builder.Services.AddOptions<K8SConfig>().BindConfiguration("Kubernetes")
            .ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddOptions<BaseYarpProxyConfig>().BindConfiguration("ReverseProxy")
            .ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddOptions<JwtSettings>().BindConfiguration("JwtSettings")
            .Validate(settings => settings.SecretKeyBytes.Length >= 32).ValidateDataAnnotations().ValidateOnStart();

        builder.Services.AddSingleton<IKubernetes>(_ =>
            new Kubernetes(
                KubernetesClientConfiguration.InClusterConfig()
            )
        );
        builder.Services.AddSingleton<IK8SEndpointSliceWatcherWorker, K8SEndpointSliceWatcherWorker>();
        builder.Services.AddSingleton<IYarpProxyConfigBuilder, YarpProxyConfigBuilder>();
        
        builder.Services.AddHostedService<K8SEndpointSliceWatcherService>();

        builder.Services.AddSingleton<IProxyConfigProvider, YarpProxyConfigProviderFromK8S.YarpProxyConfigProviderFromK8S>();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        builder.Services.ConfigureOptions<ConfigureJwtBearerOptions>();
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("AccountPolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(JwtRegisteredClaimNames.Sub);
            });
        
        var app = builder.Build();

        app.UseGrpcWeb();

        app.MapReverseProxy(applicationBuilder => applicationBuilder.UseGrpcWeb());

        app.Run();
    }
}