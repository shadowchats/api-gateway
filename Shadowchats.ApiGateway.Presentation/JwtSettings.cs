using System.ComponentModel.DataAnnotations;

namespace Shadowchats.ApiGateway.Presentation;

public record JwtSettings
{
    [Required]
    public required string SecretKeyBase64
    {
        get => _secretKeyBase64;
        init
        {
            _secretKeyBase64 = value;
            SecretKeyBytes = Convert.FromBase64String(value);
        }
    }

    public byte[] SecretKeyBytes { get; private init; } = null!;
    
    [Required]
    public required string Issuer { get; init; }
    
    [Required]
    public required string Audience { get; init; }

    private readonly string _secretKeyBase64 = null!;
}