// Shadowchats — Copyright (C) 2025
// Dorovskoy Alexey Vasilievich (One290 / 0ne290) <lenya.dorovskoy@mail.ru>
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version. See the LICENSE file for details.
// For full copyright and authorship information, see the COPYRIGHT file.

using Serilog.Core;
using Serilog.Events;
using Shadowchats.ApiGateway.Presentation.Extensions;

namespace Shadowchats.ApiGateway.Presentation.Logging.Enrichers;

public class ExceptionDetailsEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Exception == null)
            return;

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ExceptionLocation",
            logEvent.Exception.GetLocation()));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ExceptionType",
            logEvent.Exception.GetType().Name));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ExceptionMessage",
            logEvent.Exception.Message));
    }
}