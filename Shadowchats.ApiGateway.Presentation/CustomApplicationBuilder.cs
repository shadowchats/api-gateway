// Shadowchats — Copyright (C) 2025
// Dorovskoy Alexey Vasilievich (One290 / 0ne290) <lenya.dorovskoy@mail.ru>
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version. See the LICENSE file for details.
// For full copyright and authorship information, see the COPYRIGHT file.

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Shadowchats.ApiGateway.Presentation.Extensions;

namespace Shadowchats.ApiGateway.Presentation;

public static class CustomApplicationBuilder
{
    public static WebApplication Build()
    {
        var builder = WebApplication.CreateBuilder();
        
        builder.WebHost.UseSetting("AllowedHosts", "*");
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(5000, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
            });
        });
        
        builder.Host.UseSerilog();

        builder.Services.Compose(builder.Configuration);
        
        builder.Services.AddHealthChecks()
            .AddCheck("health", () => HealthCheckResult.Healthy())
            .AddCheck<ReadyHealthCheck>("ready");
        
        var app = builder.Build();
        
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = check => check.Name == "health"
        });
        app.MapHealthChecks("/ready", new HealthCheckOptions
        {
            Predicate = check => check.Name == "ready"
        });

        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        lifetime.ApplicationStopped.Register(Log.CloseAndFlush);

        app.UseSerilogRequestLogging();

        app.UseGrpcWeb();

        app.MapReverseProxy(applicationBuilder => applicationBuilder.UseGrpcWeb());

        return app;
    }
}