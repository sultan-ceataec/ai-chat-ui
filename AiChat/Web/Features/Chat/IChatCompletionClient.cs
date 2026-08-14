namespace Web.Features.Chat;

public interface IChatCompletionClient
{
    IAsyncEnumerable<ChatStreamPart> StreamTurnAsync(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ChatToolDefinition> tools,
        CancellationToken cancellationToken = default);
}
