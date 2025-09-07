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