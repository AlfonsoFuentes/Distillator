namespace Distillator.Domain.Policies;

public static class ProjectAccessFailurePolicy
{
    public static bool IsAccessDenied(IEnumerable<string> messages)
    {
        return messages.Any(message =>
            message.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("not found or access denied", StringComparison.OrdinalIgnoreCase));
    }
}
