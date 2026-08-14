namespace Web.Features.Chat;

public sealed record ChatMessage(
    ChatRole Role,
    string Content,
    string? ToolCallId = null,
    IReadOnlyList<ChatToolCall>? ToolCalls = null);
