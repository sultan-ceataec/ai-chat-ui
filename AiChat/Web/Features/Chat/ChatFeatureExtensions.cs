using System.ClientModel;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace Web.Features.Chat;

public static class ChatFeatureExtensions
{
    public static IServiceCollection AddChatFeature(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ChatOptions>(configuration.GetSection(ChatOptions.SectionName));

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ChatOptions>>().Value;

            if (string.IsNullOrWhiteSpace(options.Endpoint))
            {
                throw new InvalidOperationException("Chat:Endpoint is required.");
            }

            if (string.IsNullOrWhiteSpace(options.Model))
            {
                throw new InvalidOperationException("Chat:Model is required.");
            }

            return new ChatClient(
                options.Model,
                new ApiKeyCredential(string.IsNullOrWhiteSpace(options.ApiKey) ? "placeholder" : options.ApiKey),
                new OpenAIClientOptions { Endpoint = new Uri(options.Endpoint) });
        });

        services.AddSingleton<IChatCompletionClient, OpenAIChatClient>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<ChatSession>();

        return services;
    }
}
