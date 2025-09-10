// Shadowchats — Copyright (C) 2025
// Dorovskoy Alexey Vasilievich (One290 / 0ne290) <lenya.dorovskoy@mail.ru>
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version. See the LICENSE file for details.
// For full copyright and authorship information, see the COPYRIGHT file.

namespace Shadowchats.ApiGateway.Presentation.Extensions;

public static class ConfigurationExtensions
{
    public static T GetRequiredValue<T>(this IConfiguration configuration, string key)
    {
        var value = configuration.GetValue<T>(key);
        if (value == null || value.Equals(default(T))) 
            throw new BugException($"Configuration value '{key}' is required but was not found.");

        return value;
    }
    
    public static T GetRequired<T>(this IConfiguration configuration, string key)
    {
        var value = configuration.GetSection(key).Get<T>();
        if (value == null)
            throw new BugException($"Configuration value '{key}' is required but was not found.");

        return value;
    }
}