namespace Web.Features.Mcp;

public static class McpFeatureExtensions
{
    public static IServiceCollection AddMcpFeature(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<McpOptions>(configuration.GetSection(McpOptions.SectionName));
        services.AddHttpClient(McpToolGateway.HttpClientName);
        services.AddSingleton<IMcpToolGateway, McpToolGateway>();

        return services;
    }
}
