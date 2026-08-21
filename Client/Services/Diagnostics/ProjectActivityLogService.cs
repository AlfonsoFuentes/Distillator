using Shared.SolverConsecutive;

namespace Client.Services.Diagnostics;

public sealed class ProjectActivityLogService : ISolverTraceSink
{
    private const int MaxEntries = 200;
    private readonly List<ProjectActivityLogEntry> _entries = new();
    private readonly object _sync = new();

    public event Action? Changed;

    public IReadOnlyList<ProjectActivityLogEntry> Entries
    {
        get
        {
            lock (_sync)
            {
                return _entries.ToList();
            }
        }
    }

    public DateTimeOffset? PendingAutosaveDueAt { get; private set; }
    public string? PendingAutosaveLabel { get; private set; }
    public DateTimeOffset? LastAutosaveCompletedAt { get; private set; }
    public bool IsSolverTraceEnabled { get; private set; }
    public bool IsStreamTraceEnabled { get; private set; }

    public void SetSolverTraceEnabled(bool isEnabled)
    {
        IsSolverTraceEnabled = false;
    }

    public void SetStreamTraceEnabled(bool isEnabled)
    {
        IsStreamTraceEnabled = false;
    }

    public void StartAutosaveCountdown(string label, TimeSpan delay)
    {
        var shouldLog = PendingAutosaveDueAt == null ||
            !string.Equals(PendingAutosaveLabel, label, StringComparison.Ordinal);

        PendingAutosaveLabel = label;
        PendingAutosaveDueAt = DateTimeOffset.Now.Add(delay);
        if (shouldLog)
        {
            Add("Autosave", $"{label} queued", $"Will run after user inactivity.");
            return;
        }

        Changed?.Invoke();
    }

    public void CompleteAutosave(string message, bool succeeded)
    {
        PendingAutosaveDueAt = null;
        PendingAutosaveLabel = null;
        LastAutosaveCompletedAt = DateTimeOffset.Now;
        Add(succeeded ? "Saved" : "Error", message, null);
    }

    public void SkipAutosave(string message, string? detail = null)
    {
        PendingAutosaveDueAt = null;
        PendingAutosaveLabel = null;
        Add("Autosave", message, detail);
    }

    public void Add(string source, string message, string? detail = null)
    {
        lock (_sync)
        {
            _entries.Insert(0, new ProjectActivityLogEntry(DateTimeOffset.Now, source, message, detail));
            if (_entries.Count > MaxEntries)
            {
                _entries.RemoveRange(MaxEntries, _entries.Count - MaxEntries);
            }
        }

        if (source is not ("Solver" or "Stream"))
        {
            Changed?.Invoke();
        }
    }

    public void TraceSolver(string message, string? detail = null)
    {
    }

    public void TraceStream(string message, string? detail = null)
    {
    }

    public void Clear()
    {
        lock (_sync)
        {
            _entries.Clear();
        }

        Changed?.Invoke();
    }
}

public sealed record ProjectActivityLogEntry(
    DateTimeOffset Timestamp,
    string Source,
    string Message,
    string? Detail);
