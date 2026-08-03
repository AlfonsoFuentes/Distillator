namespace Distillator.Domain.Services;

public sealed record ProjectHydrationRequest(long Revision, Guid ProjectId);

public sealed class ProjectHydrationPublicationGate
{
    private readonly object _sync = new();
    private long _latestRevision;

    public ProjectHydrationRequest Begin(Guid projectId)
    {
        lock (_sync)
        {
            return new ProjectHydrationRequest(++_latestRevision, projectId);
        }
    }

    public bool CanPublish(ProjectHydrationRequest request, Guid hydratedProjectId)
    {
        lock (_sync)
        {
            return request.Revision == _latestRevision && request.ProjectId == hydratedProjectId;
        }
    }
}
