using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using ChatMessage = Web.Features.Chat.ChatMessage;

namespace Web.Features.Chat;

public sealed class OpenAIChatClient : IChatCompletionClient
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<OpenAIChatClient> _logger;

    public OpenAIChatClient(ChatClient chatClient, ILogger<OpenAIChatClient> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async IAsyncEnumerable<string> StreamAsync(
        IReadOnlyList<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Streaming chat completion for {MessageCount} messages.", messages.Count);

        var openAiMessages = messages.Select(ToOpenAiMessage).ToList();
        var chunkCount = 0;

        await foreach (var update in _chatClient.CompleteChatStreamingAsync(openAiMessages).WithCancellation(cancellationToken))
        {
            foreach (var part in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(part.Text))
                {
                    chunkCount++;
                    yield return part.Text;
                }
            }
        }

        _logger.LogDebug("Chat completion stream closed. Chunks={ChunkCount}.", chunkCount);
    }

    private static OpenAI.Chat.ChatMessage ToOpenAiMessage(ChatMessage message) => message.Role switch
    {
        ChatRole.System => new SystemChatMessage(message.Content),
        ChatRole.User => new UserChatMessage(message.Content),
        ChatRole.Assistant => new AssistantChatMessage(message.Content),
        _ => throw new ArgumentOutOfRangeException(nameof(message), message.Role, "Unknown chat role.")
    };
}
