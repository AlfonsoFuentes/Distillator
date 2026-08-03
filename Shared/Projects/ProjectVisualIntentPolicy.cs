namespace Shared.Projects;

public static class ProjectVisualIntentPolicy
{
    public static bool CanReapplyExistingElementVisuals(
        IEnumerable<Guid> authoritativeElementIds,
        IEnumerable<Guid> intendedElementIds)
    {
        var authoritative = authoritativeElementIds
            .Where(id => id != Guid.Empty)
            .ToHashSet();

        return intendedElementIds
            .Where(id => id != Guid.Empty)
            .All(authoritative.Contains);
    }
}
