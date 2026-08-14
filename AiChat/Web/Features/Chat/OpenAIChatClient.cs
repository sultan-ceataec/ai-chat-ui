using System.Runtime.CompilerServices;
using System.Text;
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

    public async IAsyncEnumerable<ChatStreamPart> StreamTurnAsync(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ChatToolDefinition> tools,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Streaming chat turn for {MessageCount} messages with {ToolCount} tools.",
            messages.Count,
            tools.Count);

        var openAiMessages = messages.Select(ToOpenAiMessage).ToList();
        var options = BuildOptions(tools);
        var assistantText = new StringBuilder();
        var toolCallAccumulators = new Dictionary<int, ToolCallAccumulator>();
        ChatFinishReason? finishReason = null;
        var chunkCount = 0;

        await foreach (var update in _chatClient
                           .CompleteChatStreamingAsync(openAiMessages, options)
                           .WithCancellation(cancellationToken))
        {
            foreach (var part in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(part.Text))
                {
                    assistantText.Append(part.Text);
                    chunkCount++;
                    yield return new ChatStreamPart(ChatStreamPartKind.Text, part.Text);
                }
            }

            foreach (var toolUpdate in update.ToolCallUpdates)
            {
                var index = toolUpdate.Index;
                if (!toolCallAccumulators.TryGetValue(index, out var accumulator))
                {
                    accumulator = new ToolCallAccumulator();
                    toolCallAccumulators[index] = accumulator;
                }

                if (!string.IsNullOrEmpty(toolUpdate.ToolCallId))
                {
                    accumulator.Id = toolUpdate.ToolCallId;
                }

                if (!string.IsNullOrEmpty(toolUpdate.FunctionName))
                {
                    accumulator.Name = toolUpdate.FunctionName;
                }

                if (toolUpdate.FunctionArgumentsUpdate is not null)
                {
                    accumulator.Arguments.Append(toolUpdate.FunctionArgumentsUpdate.ToString());
                }
            }

            if (update.FinishReason.HasValue)
            {
                finishReason = update.FinishReason.Value;
            }
        }

        if (finishReason == ChatFinishReason.ToolCalls && toolCallAccumulators.Count > 0)
        {
            var toolCalls = toolCallAccumulators
                .OrderBy(pair => pair.Key)
                .Select(pair => new ChatToolCall(
                    pair.Value.Id ?? string.Empty,
                    pair.Value.Name ?? string.Empty,
                    pair.Value.Arguments.ToString()))
                .ToList();

            _logger.LogDebug(
                "Chat turn completed with {ToolCallCount} tool calls.",
                toolCalls.Count);

            yield return new ChatStreamPart(
                ChatStreamPartKind.ToolCallsCompleted,
                ToolCalls: toolCalls,
                AssistantText: assistantText.Length > 0 ? assistantText.ToString() : null);
        }
        else
        {
            _logger.LogDebug(
                "Chat turn completed with text only. Chunks={ChunkCount} FinishReason={FinishReason}.",
                chunkCount,
                finishReason);
        }
    }

    private static ChatCompletionOptions BuildOptions(IReadOnlyList<ChatToolDefinition> tools)
    {
        var options = new ChatCompletionOptions();

        if (tools.Count == 0)
        {
            return options;
        }

        foreach (var tool in tools)
        {
            var parameters = string.IsNullOrWhiteSpace(tool.ParametersJson)
                ? BinaryData.FromString("""{"type":"object","properties":{}}""")
                : BinaryData.FromString(tool.ParametersJson);

            options.Tools.Add(
                ChatTool.CreateFunctionTool(
                    tool.Name,
                    tool.Description,
                    parameters));
        }

        return options;
    }

    private static OpenAI.Chat.ChatMessage ToOpenAiMessage(ChatMessage message) => message.Role switch
    {
        ChatRole.System => new SystemChatMessage(message.Content),
        ChatRole.User => new UserChatMessage(message.Content),
        ChatRole.Assistant when message.ToolCalls is { Count: > 0 } =>
            new AssistantChatMessage(message.ToolCalls.Select(ToOpenAiToolCall)),
        ChatRole.Assistant => new AssistantChatMessage(message.Content),
        ChatRole.Tool => new ToolChatMessage(message.ToolCallId!, message.Content),
        _ => throw new ArgumentOutOfRangeException(nameof(message), message.Role, "Unknown chat role."),
    };

    private static OpenAI.Chat.ChatToolCall ToOpenAiToolCall(ChatToolCall toolCall)
    {
        var arguments = string.IsNullOrWhiteSpace(toolCall.ArgumentsJson)
            ? BinaryData.FromString("{}")
            : BinaryData.FromString(toolCall.ArgumentsJson);

        return OpenAI.Chat.ChatToolCall.CreateFunctionToolCall(
            toolCall.Id,
            toolCall.Name,
            arguments);
    }

    private sealed class ToolCallAccumulator
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public StringBuilder Arguments { get; } = new();
    }
}
