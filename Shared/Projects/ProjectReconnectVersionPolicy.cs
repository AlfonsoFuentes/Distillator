namespace Shared.Projects;

public static class ProjectReconnectVersionPolicy
{
    public static bool ShouldCatchUp(Guid? knownProjectId, long knownVersion, Guid loadedProjectId, long loadedVersion)
    {
        return !ProjectVersionConfirmation.IsKnown(
            knownProjectId,
            knownVersion,
            loadedProjectId,
            loadedVersion);
    }
}
