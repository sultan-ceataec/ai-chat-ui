namespace McpServer.Features.Todos;

public static class TodoFeatureExtensions
{
    public static IServiceCollection AddTodoFeature(this IServiceCollection services)
    {
        services.AddSingleton<ITodoRepository, InMemoryTodoRepository>();
        return services;
    }
}
