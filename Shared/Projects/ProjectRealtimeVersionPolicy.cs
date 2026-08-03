namespace Shared.Projects;

public static class ProjectRealtimeVersionPolicy
{
    public static bool ShouldIgnoreEvent(
        Guid? knownProjectId,
        long knownVersion,
        Guid eventProjectId,
        long eventVersion)
    {
        return ProjectVersionConfirmation.IsKnown(
            knownProjectId,
            knownVersion,
            eventProjectId,
            eventVersion);
    }

    public static bool CanPublishLoadedDocument(
        Guid? knownProjectId,
        long knownVersion,
        Guid eventProjectId,
        long eventVersion,
        Guid loadedProjectId,
        long loadedVersion)
    {
        if (loadedProjectId != eventProjectId)
        {
            return false;
        }

        if (loadedVersion < eventVersion)
        {
            return false;
        }

        return !ProjectVersionConfirmation.IsKnown(
            knownProjectId,
            knownVersion,
            loadedProjectId,
            loadedVersion);
    }
}
