namespace Shadowchats.ApiGateway.Presentation.YarpProxyConfigProviderFromK8S;

public interface IYarpProxyConfigBuilder
{
    YarpProxyConfig Build(CancellationTokenSource changeTokenSource);
}