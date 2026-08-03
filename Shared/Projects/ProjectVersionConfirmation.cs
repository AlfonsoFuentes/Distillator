namespace Shared.Projects;

public static class ProjectVersionConfirmation
{
    public static long Confirm(long knownVersion, long confirmedVersion)
    {
        return confirmedVersion > knownVersion
            ? confirmedVersion
            : knownVersion;
    }

    public static bool IsKnown(Guid? knownProjectId, long knownVersion, Guid projectId, long version)
    {
        return knownProjectId == projectId && version <= knownVersion;
    }
}
