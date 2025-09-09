using System.ComponentModel.DataAnnotations;

namespace Shadowchats.ApiGateway.Presentation.YarpProxyConfigProviderFromK8S;

public record K8SConfig
{
    [Required]
    public required string Namespace { get; init; }
    
    [Required]
    [MinLength(1)]
    public required IReadOnlyList<string> ServiceNames { get; init; }
}