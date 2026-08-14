using System.Collections.Concurrent;

namespace McpServer.Features.Todos;

public sealed class InMemoryTodoRepository : ITodoRepository
{
    private readonly ConcurrentDictionary<Guid, Todo> _todos = new();

    public InMemoryTodoRepository()
    {
        Seed();
    }

    public IReadOnlyList<Todo> GetAll(bool? done = null)
    {
        var query = _todos.Values.AsEnumerable();

        if (done is { } filter)
        {
            query = query.Where(todo => todo.Done == filter);
        }

        return query.OrderBy(todo => todo.Done).ThenBy(todo => todo.Title).ToList();
    }

    public Todo? GetById(Guid id) => _todos.TryGetValue(id, out var todo) ? todo : null;

    public Todo Add(string title)
    {
        var todo = new Todo(Guid.NewGuid(), title.Trim(), Done: false);
        _todos[todo.Id] = todo;
        return todo;
    }

    public Todo? Complete(Guid id)
    {
        if (!_todos.TryGetValue(id, out var existing))
        {
            return null;
        }

        var completed = existing with { Done = true };
        _todos[id] = completed;
        return completed;
    }

    public bool Delete(Guid id) => _todos.TryRemove(id, out _);

    public (int Total, int Done, int Pending) GetStats()
    {
        var todos = _todos.Values.ToList();
        var done = todos.Count(todo => todo.Done);
        return (todos.Count, done, todos.Count - done);
    }

    private void Seed()
    {
        _todos[Guid.Parse("11111111-1111-1111-1111-111111111101")] =
            new Todo(Guid.Parse("11111111-1111-1111-1111-111111111101"), "Buy groceries", false);

        _todos[Guid.Parse("11111111-1111-1111-1111-111111111102")] =
            new Todo(Guid.Parse("11111111-1111-1111-1111-111111111102"), "Call dentist", true);
    }
}
