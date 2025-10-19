// Shadowchats — Copyright (C) 2025
// Dorovskoy Alexey Vasilievich (One290 / 0ne290) <lenya.dorovskoy@mail.ru>
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version. See the LICENSE file for details.
// For full copyright and authorship information, see the COPYRIGHT file.

using Yarp.ReverseProxy.Configuration;

namespace Shadowchats.ApiGateway.Presentation.YarpProxyConfigProviderFromK8S;

public class YarpProxyConfigProviderFromK8S : IProxyConfigProvider, IDisposable
{
    public YarpProxyConfigProviderFromK8S(IK8SEndpointSliceWatcherWorker k8SEndpointSliceWatcherWorker,
        IYarpProxyConfigBuilder builder)
    {
        _k8SEndpointSliceWatcherWorker = k8SEndpointSliceWatcherWorker;
        _builder = builder;

        _changeTokenSource = new CancellationTokenSource();
        _locker = new Lock();

        _k8SEndpointSliceWatcherWorker.EndpointSlicesUpdated += OnEndpointSlicesUpdated;
        _current = _builder.Build(_changeTokenSource);
    }

    public IProxyConfig GetConfig()
    {
        lock (_locker)
            return _current;
    }

    public void Dispose()
    {
        _k8SEndpointSliceWatcherWorker.EndpointSlicesUpdated -= OnEndpointSlicesUpdated;
        _changeTokenSource.Cancel();
        _changeTokenSource.Dispose();
    }

    private void OnEndpointSlicesUpdated()
    {
        lock (_locker)
        {
            var newChangeTokenSource = new CancellationTokenSource();
            var newConfig = _builder.Build(newChangeTokenSource);
            var oldChangeTokenSource = _changeTokenSource;

            _current = newConfig;
            _changeTokenSource = newChangeTokenSource;

            oldChangeTokenSource.Cancel();
            oldChangeTokenSource.Dispose();
        }
    }

    private readonly IK8SEndpointSliceWatcherWorker _k8SEndpointSliceWatcherWorker;

    private readonly IYarpProxyConfigBuilder _builder;

    private CancellationTokenSource _changeTokenSource;

    private readonly Lock _locker;

    private YarpProxyConfig _current;
}