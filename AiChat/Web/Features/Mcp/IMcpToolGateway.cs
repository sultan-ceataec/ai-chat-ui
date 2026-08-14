using Web.Features.Chat;

namespace Web.Features.Mcp;

public interface IMcpToolGateway
{
    Task<IReadOnlyList<ChatToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default);

    Task<string> CallToolAsync(
        string toolName,
        string argumentsJson,
        CancellationToken cancellationToken = default);
}
