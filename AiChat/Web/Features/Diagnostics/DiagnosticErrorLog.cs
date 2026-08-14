namespace Web.Features.Diagnostics;

public sealed class DiagnosticErrorLog
{
    private const int MaxEntries = 50;

    private readonly object _lock = new();
    private readonly List<DiagnosticError> _entries = new(MaxEntries);

    public event Action? Changed;

    public void Add(DiagnosticError error)
    {
        lock (_lock)
        {
            _entries.Add(error);

            if (_entries.Count > MaxEntries)
            {
                _entries.RemoveAt(0);
            }
        }

        Changed?.Invoke();
    }

    public IReadOnlyList<DiagnosticError> Snapshot()
    {
        lock (_lock)
        {
            return _entries
                .OrderByDescending(entry => entry.UtcNow)
                .ToList();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
        }

        Changed?.Invoke();
    }
}
