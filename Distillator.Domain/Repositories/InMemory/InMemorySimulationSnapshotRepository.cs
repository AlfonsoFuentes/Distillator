namespace Distillator.Domain.Repositories.InMemory;

public class InMemorySimulationSnapshotRepository : ISimulationSnapshotRepository
{
    private readonly Dictionary<Guid, Models.ISimulationSnapshot> _snapshots = new();

    public Task<Models.ISimulationSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _snapshots.TryGetValue(id, out var snapshot);
        return Task.FromResult(snapshot);
    }

    public Task SaveAsync(Models.ISimulationSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        _snapshots[snapshot.Id] = snapshot;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<Models.ISimulationSnapshot>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var result = _snapshots.Values
            .Where(s => s.ProjectId == projectId)
            .ToList()
            .AsReadOnly();
        return Task.FromResult<IReadOnlyCollection<Models.ISimulationSnapshot>>(result);
    }
}
