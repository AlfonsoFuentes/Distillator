namespace Distillator.Domain.Session;

public sealed record ProjectSessionSnapshot(
    Guid? ProjectId,
    Guid? FlowsheetId,
    bool HasPresence)
{
    public bool HasActiveProject => ProjectId.HasValue;
}

public static class ProjectSessionSnapshotRules
{
    public static ProjectSessionSnapshot Clear(ProjectSessionSnapshot snapshot)
    {
        return snapshot with
        {
            ProjectId = null,
            FlowsheetId = null,
            HasPresence = false
        };
    }
}
