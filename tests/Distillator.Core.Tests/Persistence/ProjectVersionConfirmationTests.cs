using Shared.Projects;

namespace Distillator.Core.Tests.Persistence;

public sealed class ProjectVersionConfirmationTests
{
    [Theory]
    [Trait("Spec", "06")]
    [Trait("Level", "Unit")]
    [InlineData(4L, 5L, 5L)]
    [InlineData(5L, 5L, 5L)]
    [InlineData(6L, 5L, 6L)]
    public void Confirm_ShouldNeverMoveKnownVersionBackwards(
        long knownVersion,
        long confirmedVersion,
        long expected)
    {
        Assert.Equal(expected, ProjectVersionConfirmation.Confirm(knownVersion, confirmedVersion));
    }

    [Fact]
    [Trait("Spec", "06")]
    [Trait("Level", "Unit")]
    public void IsKnown_ShouldTreatConfirmedHttpVersionAsKnownForRealtimeFiltering()
    {
        var projectId = Guid.Parse("e1ab7646-5963-4627-a21d-b2ef35e9f6db");

        Assert.True(ProjectVersionConfirmation.IsKnown(projectId, 7, projectId, 7));
        Assert.True(ProjectVersionConfirmation.IsKnown(projectId, 7, projectId, 6));
        Assert.False(ProjectVersionConfirmation.IsKnown(projectId, 7, projectId, 8));
        Assert.False(ProjectVersionConfirmation.IsKnown(Guid.NewGuid(), 7, projectId, 7));
    }
}
