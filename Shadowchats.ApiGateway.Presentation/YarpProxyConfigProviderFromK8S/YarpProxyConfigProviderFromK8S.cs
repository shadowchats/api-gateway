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
        IYarpProxyConfigBuilder builder, ILogger<YarpProxyConfigProviderFromK8S> logger)
    {
        _k8SEndpointSliceWatcherWorker = k8SEndpointSliceWatcherWorker;
        _builder = builder;
        _logger = logger;

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
        var operationId = Guid.NewGuid().ToString("N").Substring(0, 8);
        _logger.LogInformation("[DEBUG_LABEL : {OperationId}] OnEndpointSlicesUpdated START. Thread: {ThreadId}",
            operationId, Environment.CurrentManagedThreadId);

        lock (_locker)
        {
            _logger.LogInformation("[DEBUG_LABEL : {OperationId}] Lock acquired", operationId);

            // 1. Создаём новый конфиг БЕЗ отмены старого токена
            _logger.LogInformation("[DEBUG_LABEL : {OperationId}] Creating new CancellationTokenSource", operationId);
            var newChangeTokenSource = new CancellationTokenSource();

            _logger.LogInformation("[DEBUG_LABEL : {OperationId}] Building config...", operationId);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var newConfig = _builder.Build(newChangeTokenSource);
            sw.Stop();
            _logger.LogInformation(
                "[DEBUG_LABEL : {OperationId}] Config built in {ElapsedMs}ms. Routes: {RouteCount}, Clusters: {ClusterCount}",
                operationId, sw.ElapsedMilliseconds, newConfig.Routes.Count, newConfig.Clusters.Count);

            // Логируем детали кластеров
            foreach (var cluster in newConfig.Clusters)
            {
                var destCount = cluster.Destinations?.Count ?? 0;
                _logger.LogInformation("[DEBUG_LABEL : {OperationId}] Cluster {ClusterId}: {DestinationCount} destinations",
                    operationId, cluster.ClusterId, destCount);
            }

            // 2. Только ПОСЛЕ того, как новый конфиг готов, отменяем старый
            _logger.LogInformation("[DEBUG_LABEL : {OperationId}] Swapping config...", operationId);
            var oldChangeTokenSource = _changeTokenSource;
            var oldClustersCount = _current?.Clusters.Count ?? 0;

            _current = newConfig;
            _changeTokenSource = newChangeTokenSource;
            _logger.LogInformation("[DEBUG_LABEL : {OperationId}] Config swapped. Old clusters: {OldCount}, New clusters: {NewCount}",
                operationId, oldClustersCount, newConfig.Clusters.Count);

            // 3. Теперь безопасно отменяем старый токен
            _logger.LogInformation("[DEBUG_LABEL : {OperationId}] Cancelling old token...", operationId);
            try
            {
                oldChangeTokenSource.Cancel();
                _logger.LogInformation("[DEBUG_LABEL : {OperationId}] Old token cancelled successfully", operationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DEBUG_LABEL : {OperationId}] Error cancelling old token", operationId);
            }

            _logger.LogInformation("[DEBUG_LABEL : {OperationId}] Disposing old token...", operationId);
            oldChangeTokenSource.Dispose();

            _logger.LogInformation("[DEBUG_LABEL : {OperationId}] OnEndpointSlicesUpdated COMPLETE", operationId);
        }
    }

    private readonly IK8SEndpointSliceWatcherWorker _k8SEndpointSliceWatcherWorker;

    private readonly IYarpProxyConfigBuilder _builder;

    private readonly ILogger<YarpProxyConfigProviderFromK8S> _logger;

    private CancellationTokenSource _changeTokenSource;

    private readonly Lock _locker;

    private YarpProxyConfig _current;
}