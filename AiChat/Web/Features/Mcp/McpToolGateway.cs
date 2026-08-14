using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Web.Features.Chat;

namespace Web.Features.Mcp;

public sealed class McpToolGateway : IMcpToolGateway
{
    public const string HttpClientName = "mcp";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly McpOptions _options;
    private readonly ILogger<McpToolGateway> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public McpToolGateway(
        IHttpClientFactory httpClientFactory,
        IOptions<McpOptions> options,
        ILogger<McpToolGateway> logger,
        ILoggerFactory loggerFactory)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public async Task<IReadOnlyList<ChatToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            _logger.LogDebug("MCP endpoint not configured; returning no tools.");
            return [];
        }

        try
        {
            await using var client = await CreateClientAsync(cancellationToken);
            var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);

            _logger.LogDebug("Listed {ToolCount} MCP tools from {Endpoint}.", tools.Count, _options.Endpoint);

            return tools
                .Select(tool => new ChatToolDefinition(
                    tool.Name,
                    tool.Description ?? string.Empty,
                    tool.JsonSchema.GetRawText()))
                .ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to list MCP tools from {Endpoint}.", _options.Endpoint);
            throw new ChatException("Failed to list MCP tools.", ex);
        }
    }

    public async Task<string> CallToolAsync(
        string toolName,
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            throw new ChatException("MCP is not configured.");
        }

        try
        {
            await using var client = await CreateClientAsync(cancellationToken);
            var arguments = ParseArguments(argumentsJson);

            _logger.LogDebug(
                "Calling MCP tool {ToolName} with arguments {Arguments}.",
                toolName,
                argumentsJson);

            var result = await client.CallToolAsync(
                toolName,
                arguments,
                cancellationToken: cancellationToken);

            var content = FlattenToolResult(result);

            _logger.LogDebug(
                "MCP tool {ToolName} completed. IsError={IsError} ContentLength={Length}.",
                toolName,
                result.IsError ?? false,
                content.Length);

            return content;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to call MCP tool {ToolName}.", toolName);
            throw new ChatException($"Failed to call MCP tool '{toolName}'.", ex);
        }
    }

    private bool IsConfigured() => !string.IsNullOrWhiteSpace(_options.Endpoint);

    private async Task<McpClient> CreateClientAsync(CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        var transportOptions = new HttpClientTransportOptions
        {
            Endpoint = new Uri(_options.Endpoint),
            TransportMode = HttpTransportMode.StreamableHttp,
            Name = "AiChat MCP",
        };

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            transportOptions.AdditionalHeaders = new Dictionary<string, string>
            {
                ["api-key"] = _options.ApiKey,
            };
        }

        var transport = new HttpClientTransport(
            transportOptions,
            httpClient,
            _loggerFactory,
            ownsHttpClient: false);

        return await McpClient.CreateAsync(transport, loggerFactory: _loggerFactory, cancellationToken: cancellationToken);
    }

    private static IReadOnlyDictionary<string, object?> ParseArguments(string argumentsJson)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson) || argumentsJson == "{}")
        {
            return new Dictionary<string, object?>();
        }

        var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);
        if (parsed is null || parsed.Count == 0)
        {
            return new Dictionary<string, object?>();
        }

        return parsed.ToDictionary(
            pair => pair.Key,
            pair => JsonElementToObject(pair.Value));
    }

    private static object? JsonElementToObject(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number when element.TryGetInt64(out var intValue) => intValue,
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToList(),
        JsonValueKind.Object => element.EnumerateObject()
            .ToDictionary(property => property.Name, property => JsonElementToObject(property.Value)),
        _ => element.GetRawText(),
    };

    private static string FlattenToolResult(CallToolResult result)
    {
        if (result.Content is null || result.Content.Count == 0)
        {
            return result.IsError == true ? "Tool returned an error." : string.Empty;
        }

        var builder = new StringBuilder();

        foreach (var block in result.Content)
        {
            if (block is TextContentBlock textBlock && !string.IsNullOrEmpty(textBlock.Text))
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(textBlock.Text);
            }
        }

        return builder.Length > 0
            ? builder.ToString()
            : result.IsError == true ? "Tool returned an error." : string.Empty;
    }
}
