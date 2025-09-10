// Shadowchats — Copyright (C) 2025
// Dorovskoy Alexey Vasilievich (One290 / 0ne290) <lenya.dorovskoy@mail.ru>
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version. See the LICENSE file for details.
// For full copyright and authorship information, see the COPYRIGHT file.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Shadowchats.ApiGateway.Presentation;

public class ConfigureJwtBearerOptions : IConfigureOptions<JwtBearerOptions>
{
    public ConfigureJwtBearerOptions(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public void Configure(JwtBearerOptions options) => options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = _settings.Issuer, ValidateAudience = true,
        ValidAudience = _settings.Audience, ValidateLifetime = true, ClockSkew = TimeSpan.Zero,
        ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(_settings.SecretKeyBytes),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
    };
    
    private readonly JwtSettings _settings;
}