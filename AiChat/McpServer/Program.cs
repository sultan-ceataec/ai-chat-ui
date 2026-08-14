using McpServer.Features.Todos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTodoFeature();
builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new()
    {
        Name = "AiChat Todo MCP",
        Version = "1.0.0",
    };
})
.WithHttpTransport(options => options.Stateless = true)
.WithTools<TodoTools>()
.WithResources<TodoResources>();

var app = builder.Build();

app.MapMcp("/mcp");

app.Run();
