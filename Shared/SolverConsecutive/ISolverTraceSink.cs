namespace Shared.SolverConsecutive;

public interface ISolverTraceSink
{
    bool IsSolverTraceEnabled { get; }
    bool IsStreamTraceEnabled { get; }
    void TraceSolver(string message, string? detail = null);
    void TraceStream(string message, string? detail = null);
}
