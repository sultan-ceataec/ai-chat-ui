namespace Web.Features.Chat;

public interface IChatCompletionClient
{
    IAsyncEnumerable<string> StreamAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken = default);
}
