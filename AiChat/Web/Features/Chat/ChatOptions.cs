namespace Web.Features.Chat;

public sealed class ChatOptions
{
    public const string SectionName = "Chat";

    public string Endpoint { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string SystemPrompt { get; set; } = "You are a helpful assistant.";
}
