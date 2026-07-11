namespace Distillator.Domain.Models
{
    public class SimulationSnapshot : ISimulationSnapshot
    {
        public Guid Id { get; }
        public Guid ProjectId { get; }
        public DateTime CreatedAt { get; }
        public bool Converged { get; }
        public TimeSpan ExecutionTime { get; }
        public string ResultsJson { get; }

        public SimulationSnapshot(Guid id, Guid projectId, DateTime createdAt, bool converged, TimeSpan executionTime, string resultsJson)
        {
            Id = id;
            ProjectId = projectId;
            CreatedAt = createdAt;
            Converged = converged;
            ExecutionTime = executionTime;
            ResultsJson = resultsJson;
        }
    }
}
