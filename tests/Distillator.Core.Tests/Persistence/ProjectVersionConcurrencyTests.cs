using Shared.Projects;

namespace Distillator.Core.Tests.Persistence;

public sealed class ProjectVersionConcurrencyTests
{
    [Theory]
    [Trait("Spec", "06")]
    [Trait("Level", "Unit")]
    [InlineData(5L, 5L, true)]
    [InlineData(5L, null, true)]
    [InlineData(5L, 4L, false)]
    [InlineData(5L, 6L, false)]
    public void IsExpectedVersionValid_ShouldAcceptOnlyMatchingKnownVersion(
        long currentVersion,
        long? expectedVersion,
        bool expected)
    {
        Assert.Equal(expected, ProjectVersionConcurrency.IsExpectedVersionValid(currentVersion, expectedVersion));
    }

    [Fact]
    [Trait("Spec", "06")]
    [Trait("Level", "Unit")]
    public void BuildConflictMessage_ShouldIncludeCurrentAndExpectedVersions()
    {
        var message = ProjectVersionConcurrency.BuildConflictMessage(12, 10);

        Assert.Contains("12", message);
        Assert.Contains("10", message);
    }

    [Theory]
    [Trait("Spec", "06")]
    [Trait("Level", "Unit")]
    [InlineData("Project version conflict. Current version is 12; expected version was 10.", true)]
    [InlineData("Connection error: offline", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsConflictMessage_ShouldIdentifyExpectedVersionConflictsOnly(string? message, bool expected)
    {
        Assert.Equal(expected, ProjectVersionConcurrency.IsConflictMessage(message));
    }
}
