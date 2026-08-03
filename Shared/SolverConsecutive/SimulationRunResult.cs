namespace Shared.SolverConsecutive;

public enum SimulationRunStatus
{
    Completed,
    Failed,
    Superseded
}

public sealed record SimulationRunResult(
    Guid RunId,
    SimulationRunStatus Status,
    bool Converged,
    IReadOnlyList<string> Diagnostics)
{
    public static SimulationRunResult Completed(Guid runId, bool converged, IReadOnlyList<string> diagnostics)
    {
        return new SimulationRunResult(runId, SimulationRunStatus.Completed, converged, diagnostics);
    }

    public static SimulationRunResult Failed(Guid runId, IReadOnlyList<string> diagnostics)
    {
        return new SimulationRunResult(runId, SimulationRunStatus.Failed, false, diagnostics);
    }

    public SimulationRunResult Supersede()
    {
        return this with { Status = SimulationRunStatus.Superseded };
    }
}
