namespace Web.Features.Chat;

public interface IChatService
{
    IAsyncEnumerable<string> StreamAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default);
}
