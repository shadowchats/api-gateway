using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace Shadowchats.ApiGateway.Presentation.YarpProxyConfigProviderFromK8S;

public record YarpProxyConfig : BaseYarpProxyConfig, IProxyConfig
{
    public required IChangeToken ChangeToken { get; init; }
}