namespace Web.Features.Diagnostics;

public sealed class DiagnosticErrorLoggerProvider : ILoggerProvider
{
    private readonly DiagnosticErrorLog _log;

    public DiagnosticErrorLoggerProvider(DiagnosticErrorLog log)
    {
        _log = log;
    }

    public ILogger CreateLogger(string categoryName) => new DiagnosticErrorLogger(_log, categoryName);

    public void Dispose()
    {
    }

    private sealed class DiagnosticErrorLogger : ILogger
    {
        private readonly DiagnosticErrorLog _log;
        private readonly string _category;

        public DiagnosticErrorLogger(DiagnosticErrorLog log, string category)
        {
            _log = log;
            _category = category;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            _log.Add(new DiagnosticError(
                DateTimeOffset.UtcNow,
                _category,
                formatter(state, exception),
                exception?.ToString()));
        }
    }
}
