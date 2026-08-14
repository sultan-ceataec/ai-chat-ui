namespace Web.Features.Mcp;

public sealed class McpOptions
{
    public const string SectionName = "Mcp";

    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
}
