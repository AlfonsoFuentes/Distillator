namespace Distillator.Domain.Repositories.InMemory;

public class InMemoryProjectRepository : IProjectRepository
{
    private readonly Dictionary<Guid, Models.Project> _projects = new();

    public Task<Models.Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _projects.TryGetValue(id, out var project);
        return Task.FromResult(project);
    }

    public Task<IReadOnlyCollection<Models.Project>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userProjects = _projects.Values
            .Where(p => p.OwnerUserId == userId)
            .ToList()
            .AsReadOnly();
        return Task.FromResult<IReadOnlyCollection<Models.Project>>(userProjects);
    }

    public Task SaveAsync(Models.Project project, CancellationToken cancellationToken = default)
    {
        _projects[project.Id] = project;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _projects.Remove(id);
        return Task.CompletedTask;
    }
}
