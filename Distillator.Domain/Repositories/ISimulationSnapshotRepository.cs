namespace Distillator.Domain.Repositories;

public interface ISimulationSnapshotRepository
{
    Task<Models.ISimulationSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveAsync(Models.ISimulationSnapshot snapshot, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Models.ISimulationSnapshot>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
}
