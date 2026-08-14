using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.Logging;
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
            var logger = sp.GetRequiredService<ILogger<OpenAIChatClient>>();

            if (string.IsNullOrWhiteSpace(options.Endpoint))
            {
                throw new InvalidOperationException("Chat:Endpoint is required.");
            }

            if (string.IsNullOrWhiteSpace(options.Model))
            {
                throw new InvalidOperationException("Chat:Model is required.");
            }

            logger.LogDebug("Chat client configured. Endpoint={Endpoint} Model={Model}", options.Endpoint, options.Model);

            var clientOptions = new OpenAIClientOptions
            {
                Endpoint = new Uri(options.Endpoint),
            };

            if (!string.IsNullOrWhiteSpace(options.ApimSubscriptionKey))
            {
                clientOptions.AddPolicy(
                    new ApimSubscriptionKeyHandler(options.ApimSubscriptionKey),
                    PipelinePosition.PerCall);
                logger.LogDebug("APIM subscription key policy enabled.");
            }

            return new ChatClient(
                options.Model,
                new ApiKeyCredential(string.IsNullOrWhiteSpace(options.ApiKey) ? "placeholder" : options.ApiKey),
                clientOptions);
        });

        services.AddSingleton<IChatCompletionClient, OpenAIChatClient>();
        services.AddSingleton<MarkdownRenderer>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<ChatSession>();

        return services;
    }
}
