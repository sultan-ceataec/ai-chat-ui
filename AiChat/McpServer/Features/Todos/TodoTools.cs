using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace McpServer.Features.Todos;

[McpServerToolType]
public sealed class TodoTools
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly ITodoRepository _repository;
    private readonly ILogger<TodoTools> _logger;

    public TodoTools(ITodoRepository repository, ILogger<TodoTools> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [McpServerTool(Name = "list_todos")]
    [Description("List todos. Optionally filter by done status.")]
    public string ListTodos(
        [Description("If set, only return todos matching this done flag.")] bool? done = null)
    {
        var todos = _repository.GetAll(done);
        _logger.LogDebug("list_todos returned {Count} todos.", todos.Count);
        return JsonSerializer.Serialize(todos, JsonOptions);
    }

    [McpServerTool(Name = "create_todo")]
    [Description("Create a new todo.")]
    public string CreateTodo(
        [Description("Todo title.")] string title)
    {
        var todo = _repository.Add(title);
        _logger.LogDebug("create_todo id={Id} title={Title}.", todo.Id, todo.Title);
        return JsonSerializer.Serialize(todo, JsonOptions);
    }

    [McpServerTool(Name = "complete_todo")]
    [Description("Mark a todo as done.")]
    public string CompleteTodo(
        [Description("Todo id as a GUID string.")] string id)
    {
        if (!Guid.TryParse(id, out var todoId))
        {
            return JsonSerializer.Serialize(new { error = "Invalid todo id." }, JsonOptions);
        }

        var todo = _repository.Complete(todoId);
        _logger.LogDebug("complete_todo id={Id} found={Found}.", id, todo is not null);
        return todo is null
            ? JsonSerializer.Serialize(new { error = $"Todo not found: {id}" }, JsonOptions)
            : JsonSerializer.Serialize(todo, JsonOptions);
    }

    [McpServerTool(Name = "delete_todo")]
    [Description("Delete a todo by id.")]
    public string DeleteTodo(
        [Description("Todo id as a GUID string.")] string id)
    {
        if (!Guid.TryParse(id, out var todoId))
        {
            return JsonSerializer.Serialize(new { error = "Invalid todo id." }, JsonOptions);
        }

        var deleted = _repository.Delete(todoId);
        _logger.LogDebug("delete_todo id={Id} deleted={Deleted}.", id, deleted);
        return deleted
            ? JsonSerializer.Serialize(new { deleted = true, id }, JsonOptions)
            : JsonSerializer.Serialize(new { error = $"Todo not found: {id}" }, JsonOptions);
    }
}
