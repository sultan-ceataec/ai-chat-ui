namespace Web.Features.Diagnostics;

public sealed record DiagnosticError(
    DateTimeOffset UtcNow,
    string Category,
    string Message,
    string? ExceptionText);
