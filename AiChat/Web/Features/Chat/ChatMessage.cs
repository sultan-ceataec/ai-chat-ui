namespace Web.Features.Chat;

public sealed record ChatMessage(ChatRole Role, string Content);
