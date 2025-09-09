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