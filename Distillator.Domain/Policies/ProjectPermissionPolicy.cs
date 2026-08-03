namespace Distillator.Domain.Policies;

public static class ProjectPermissionPolicy
{
    public static bool CanEdit(bool isOwner, string? role)
    {
        return isOwner ||
               string.Equals(role, "Owner", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, "Editor", StringComparison.OrdinalIgnoreCase);
    }

    public static bool CanManage(bool isOwner, string? role)
    {
        return isOwner ||
               string.Equals(role, "Owner", StringComparison.OrdinalIgnoreCase);
    }
}
