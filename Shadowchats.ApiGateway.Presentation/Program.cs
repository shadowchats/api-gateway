// Shadowchats — Copyright (C) 2025
// Dorovskoy Alexey Vasilievich (One290 / 0ne290) <lenya.dorovskoy@mail.ru>
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version. See the LICENSE file for details.
// For full copyright and authorship information, see the COPYRIGHT file.

using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using k8s;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Shadowchats.ApiGateway.Presentation.YarpProxyConfigProviderFromK8S;
using Yarp.ReverseProxy.Configuration;

namespace Shadowchats.ApiGateway.Presentation;

public static class Program
{
    public static void Main()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        
        var builder = WebApplication.CreateBuilder();
        
        builder.WebHost.UseSetting("AllowedHosts", "*");
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(5000, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
            });
        });

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