namespace Shared.Projects;

public static class ProjectOperationId
{
    public static bool IsSpecified(Guid? operationId)
    {
        return operationId.HasValue && operationId.Value != Guid.Empty;
    }

    public static Guid? Normalize(Guid? operationId)
    {
        return IsSpecified(operationId) ? operationId : null;
    }
}
