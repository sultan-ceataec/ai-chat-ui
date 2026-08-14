using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Features.Todos;

[McpServerResourceType]
public sealed class TodoResources
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly ITodoRepository _repository;
    private readonly ILogger<TodoResources> _logger;

    public TodoResources(ITodoRepository repository, ILogger<TodoResources> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [McpServerResource(
        UriTemplate = "todo://catalog",
        Name = "Todo Catalog",
        MimeType = "application/json")]
    [Description("All todos as JSON.")]
    public string GetCatalog()
    {
        var todos = _repository.GetAll();
        _logger.LogDebug("Resource todo://catalog returned {Count} todos.", todos.Count);
        return JsonSerializer.Serialize(todos, JsonOptions);
    }

    [McpServerResource(
        UriTemplate = "todo://stats",
        Name = "Todo Stats",
        MimeType = "application/json")]
    [Description("Total, done, and pending counts.")]
    public string GetStats()
    {
        var (total, done, pending) = _repository.GetStats();
        _logger.LogDebug("Resource todo://stats total={Total}.", total);
        return JsonSerializer.Serialize(new { total, done, pending }, JsonOptions);
    }

    [McpServerResource(
        UriTemplate = "todo://{id}",
        Name = "Todo Item",
        MimeType = "application/json")]
    [Description("A single todo by GUID id.")]
    public ResourceContents GetTodoById(string id)
    {
        if (!Guid.TryParse(id, out var todoId))
        {
            throw new McpException($"Invalid todo id: {id}");
        }

        var todo = _repository.GetById(todoId)
            ?? throw new McpException($"Todo not found: {id}");

        _logger.LogDebug("Resource todo://{Id} found.", id);

        return new TextResourceContents
        {
            Uri = $"todo://{id}",
            MimeType = "application/json",
            Text = JsonSerializer.Serialize(todo, JsonOptions),
        };
    }
}
