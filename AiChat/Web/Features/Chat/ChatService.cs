using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Web.Features.Mcp;

namespace Web.Features.Chat;

public sealed class ChatService : IChatService
{
    private const int MaxToolRounds = 8;

    private readonly IChatCompletionClient _client;
    private readonly IMcpToolGateway _mcpGateway;
    private readonly ChatOptions _options;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IChatCompletionClient client,
        IMcpToolGateway mcpGateway,
        IOptions<ChatOptions> options,
        ILogger<ChatService> logger)
    {
        _client = client;
        _mcpGateway = mcpGateway;
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

        var workingMessages = BuildMessages(history);
        var tools = await _mcpGateway.ListToolsAsync(cancellationToken);

        _logger.LogDebug(
            "Starting chat with {MessageCount} messages and {ToolCount} MCP tools.",
            workingMessages.Count,
            tools.Count);

        for (var round = 0; round < MaxToolRounds; round++)
        {
            IReadOnlyList<ChatToolCall>? toolCalls = null;
            var textParts = new List<string>();

            var turnEnumerator = _client
                .StreamTurnAsync(workingMessages, tools, cancellationToken)
                .GetAsyncEnumerator(cancellationToken);

            try
            {
                while (true)
                {
                    bool moved;
                    try
                    {
                        moved = await turnEnumerator.MoveNextAsync();
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogDebug("Chat stream cancelled during tool round {Round}.", round);
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to generate a chat response.");
                        throw new ChatException("Failed to generate a chat response.", ex);
                    }

                    if (!moved)
                    {
                        break;
                    }

                    var part = turnEnumerator.Current;
                    switch (part.Kind)
                    {
                        case ChatStreamPartKind.Text:
                            textParts.Add(part.Text!);
                            break;
                        case ChatStreamPartKind.ToolCallsCompleted:
                            toolCalls = part.ToolCalls;
                            break;
                    }
                }
            }
            finally
            {
                await turnEnumerator.DisposeAsync();
            }

            if (toolCalls is { Count: > 0 })
            {
                _logger.LogDebug(
                    "Tool round {Round} completed with {ToolCallCount} calls.",
                    round,
                    toolCalls.Count);

                var assistantContent = string.Concat(textParts);
                workingMessages.Add(new ChatMessage(
                    ChatRole.Assistant,
                    assistantContent,
                    ToolCalls: toolCalls));

                foreach (var call in toolCalls)
                {
                    var result = await _mcpGateway.CallToolAsync(
                        call.Name,
                        call.ArgumentsJson,
                        cancellationToken);

                    workingMessages.Add(new ChatMessage(
                        ChatRole.Tool,
                        result,
                        ToolCallId: call.Id));
                }

                continue;
            }

            _logger.LogDebug(
                "Final chat turn completed after {RoundCount} tool rounds. Tokens={TokenCount}.",
                round,
                textParts.Count);

            foreach (var textPart in textParts)
            {
                yield return textPart;
            }

            yield break;
        }

        _logger.LogWarning("Reached maximum tool rounds ({MaxToolRounds}).", MaxToolRounds);
        throw new ChatException("The assistant exceeded the maximum number of tool calls.");
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
