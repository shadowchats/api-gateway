// Shadowchats — Copyright (C) 2025
// Dorovskoy Alexey Vasilievich (One290 / 0ne290) <lenya.dorovskoy@mail.ru>
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version. See the LICENSE file for details.
// For full copyright and authorship information, see the COPYRIGHT file.

using System.Collections.Concurrent;

namespace Shadowchats.ApiGateway.Presentation.YarpProxyConfigProviderFromK8S;

public record K8SServiceState
{
    public K8SServiceState()
    {
        EndpointSliceStates = new ConcurrentDictionary<string, K8SEndpointSliceState>();
    }
    
    public ConcurrentDictionary<string, K8SEndpointSliceState> EndpointSliceStates { get; }
        
    public IReadOnlyList<string> AllBackends =>
        EndpointSliceStates.Values
            .SelectMany(s => s.Backends)
            .Distinct()
            .ToList();
}