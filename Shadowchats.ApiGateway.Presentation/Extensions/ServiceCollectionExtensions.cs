// Shadowchats — Copyright (C) 2025
// Dorovskoy Alexey Vasilievich (One290 / 0ne290) <lenya.dorovskoy@mail.ru>
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version. See the LICENSE file for details.
// For full copyright and authorship information, see the COPYRIGHT file.

using System.IdentityModel.Tokens.Jwt;
using k8s;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;
using Shadowchats.ApiGateway.Presentation.YarpProxyConfigProviderFromK8S;
using Yarp.ReverseProxy.Configuration;

namespace Shadowchats.ApiGateway.Presentation.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection Compose(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddReverseProxy();

        services.AddOptions<K8SConfig>().BindConfiguration("Kubernetes")
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<BaseYarpProxyConfig>().BindConfiguration("ReverseProxy")
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<JwtSettings>().BindConfiguration("JwtSettings")
            .Validate(settings => settings.SecretKeyBytes.Length >= 32).ValidateDataAnnotations().ValidateOnStart();

        services.AddSingleton<IKubernetes>(_ =>
            new Kubernetes(
                KubernetesClientConfiguration.InClusterConfig()
            )
        );
        services.AddSingleton<IK8SEndpointSliceWatcherWorker, K8SEndpointSliceWatcherWorker>();
        services.AddSingleton<IYarpProxyConfigBuilder, YarpProxyConfigBuilder>();
        
        services.AddHostedService<K8SEndpointSliceWatcherService>();

        services.AddSingleton<IProxyConfigProvider, YarpProxyConfigProviderFromK8S.YarpProxyConfigProviderFromK8S>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.ConfigureOptions<ConfigureJwtBearerOptions>();
        services.AddAuthorizationBuilder()
            .AddPolicy("AccountPolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(JwtRegisteredClaimNames.Sub);
            });
        
        services.AddOpenTelemetry().WithTracing(tracerProviderBuilder =>
        {
            tracerProviderBuilder.AddAspNetCoreInstrumentation(options => { options.RecordException = true; })
                .AddGrpcClientInstrumentation().AddHttpClientInstrumentation();
        });

        return services.AddCustomLogging();
    }
    
    private static IServiceCollection AddCustomLogging(this IServiceCollection services)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .Enrich.WithSpan()
            .MinimumLevel.Information()
            .WriteTo.Async(c => c.Console(new Serilog.Formatting.Json.JsonFormatter()))
            .CreateLogger();

        services.AddSerilog();

        return services;
    }
}
