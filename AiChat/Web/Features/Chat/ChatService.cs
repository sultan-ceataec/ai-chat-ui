using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Web.Features.Chat;

public sealed class ChatService : IChatService
{
    private readonly IChatCompletionClient _client;
    private readonly ChatOptions _options;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IChatCompletionClient client,
        IOptions<ChatOptions> options,
        ILogger<ChatService> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public async IAsyncEnumerable<string> StreamAsync(
        IReadOnlyList<ChatMessage> history,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Building chat request. HistoryMessages={HistoryCount} Model={Model}",
            history.Count,
            _options.Model);

        var messages = BuildMessages(history);
        _logger.LogDebug("Sending {MessageCount} messages to the chat client.", messages.Count);

        var enumerator = _client.StreamAsync(messages, cancellationToken).GetAsyncEnumerator(cancellationToken);
        var tokenCount = 0;

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
                    _logger.LogDebug("Chat stream cancelled after {TokenCount} tokens.", tokenCount);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate a chat response.");
                    throw new ChatException("Failed to generate a chat response.", ex);
                }

                if (!moved)
                {
                    _logger.LogDebug("Chat stream finished. Tokens={TokenCount}.", tokenCount);
                    yield break;
                }

                tokenCount++;
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
