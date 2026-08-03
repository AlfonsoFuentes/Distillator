namespace Shared.Projects;

public static class ProjectVersionConcurrency
{
    public const string ConflictMessagePrefix = "Project version conflict.";

    public static bool IsExpectedVersionValid(long currentVersion, long? expectedVersion)
    {
        return !expectedVersion.HasValue || expectedVersion.Value == currentVersion;
    }

    public static string BuildConflictMessage(long currentVersion, long expectedVersion)
    {
        return $"{ConflictMessagePrefix} Current version is {currentVersion}; expected version was {expectedVersion}.";
    }

    public static bool IsConflictMessage(string? message)
    {
        return !string.IsNullOrWhiteSpace(message)
            && message.StartsWith(ConflictMessagePrefix, StringComparison.Ordinal);
    }
}
