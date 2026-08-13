using System.Runtime.CompilerServices;
using OpenAI.Chat;
using ChatMessage = Web.Features.Chat.ChatMessage;

namespace Web.Features.Chat;

public sealed class OpenAIChatClient : IChatCompletionClient
{
    private readonly ChatClient _chatClient;

    public OpenAIChatClient(ChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public async IAsyncEnumerable<string> StreamAsync(
        IReadOnlyList<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var openAiMessages = messages.Select(ToOpenAiMessage).ToList();

        await foreach (var update in _chatClient.CompleteChatStreamingAsync(openAiMessages).WithCancellation(cancellationToken))
        {
            foreach (var part in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(part.Text))
                {
                    yield return part.Text;
                }
            }
        }
    }

    private static OpenAI.Chat.ChatMessage ToOpenAiMessage(ChatMessage message) => message.Role switch
    {
        ChatRole.System => new SystemChatMessage(message.Content),
        ChatRole.User => new UserChatMessage(message.Content),
        ChatRole.Assistant => new AssistantChatMessage(message.Content),
        _ => throw new ArgumentOutOfRangeException(nameof(message), message.Role, "Unknown chat role.")
    };
}
