using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

namespace Web.Features.Chat;

public sealed class ChatService : IChatService
{
    private readonly IChatCompletionClient _client;
    private readonly ChatOptions _options;

    public ChatService(IChatCompletionClient client, IOptions<ChatOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async IAsyncEnumerable<string> StreamAsync(
        IReadOnlyList<ChatMessage> history,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = BuildMessages(history);
        var enumerator = _client.StreamAsync(messages, cancellationToken).GetAsyncEnumerator(cancellationToken);

        try
        {
            while (true)
            {
                bool moved;
                try
                {
                    moved = await enumerator.MoveNextAsync();
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new ChatException("Failed to generate a chat response.", ex);
                }

                if (!moved)
                {
                    yield break;
                }

                yield return enumerator.Current;
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }

    private List<ChatMessage> BuildMessages(IReadOnlyList<ChatMessage> history)
    {
        var messages = new List<ChatMessage>(history.Count + 1);

        if (!string.IsNullOrWhiteSpace(_options.SystemPrompt))
        {
            messages.Add(new ChatMessage(ChatRole.System, _options.SystemPrompt));
        }

        foreach (var message in history)
        {
            if (message.Role == ChatRole.System)
            {
                continue;
            }

            messages.Add(message);
        }

        return messages;
    }
}
