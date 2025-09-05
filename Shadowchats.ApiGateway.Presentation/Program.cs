namespace Shadowchats.ApiGateway.Presentation;

public static class Program
{
    public static void Main()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
        var app = builder.Build();
        app.MapReverseProxy();
        app.Run();
    }
}