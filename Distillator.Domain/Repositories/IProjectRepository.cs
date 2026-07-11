namespace Distillator.Domain.Repositories;

public interface IProjectRepository
{
    Task<Models.Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Models.Project>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SaveAsync(Models.Project project, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
