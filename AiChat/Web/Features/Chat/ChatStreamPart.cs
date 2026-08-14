namespace Web.Features.Chat;

public enum ChatStreamPartKind
{
    Text,
    ToolCallsCompleted,
}

public sealed record ChatStreamPart(
    ChatStreamPartKind Kind,
    string? Text = null,
    IReadOnlyList<ChatToolCall>? ToolCalls = null,
    string? AssistantText = null);
