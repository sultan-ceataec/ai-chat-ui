namespace McpServer.Features.Todos;

public interface ITodoRepository
{
    IReadOnlyList<Todo> GetAll(bool? done = null);

    Todo? GetById(Guid id);

    Todo Add(string title);

    Todo? Complete(Guid id);

    bool Delete(Guid id);

    (int Total, int Done, int Pending) GetStats();
}
