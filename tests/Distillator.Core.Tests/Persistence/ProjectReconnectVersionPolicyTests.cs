using Shared.Projects;

namespace Distillator.Core.Tests.Persistence;

public sealed class ProjectReconnectVersionPolicyTests
{
    [Fact]
    [Trait("Spec", "07")]
    [Trait("Level", "Unit")]
    public void ShouldCatchUp_WhenLoadedVersionIsNewer_ShouldReturnTrue()
    {
        var projectId = Guid.Parse("92eadcb8-2d8a-4ce5-8e54-04dfd878935f");

        Assert.True(ProjectReconnectVersionPolicy.ShouldCatchUp(projectId, 4, projectId, 5));
    }

    [Fact]
    [Trait("Spec", "07")]
    [Trait("Level", "Unit")]
    public void ShouldCatchUp_WhenLoadedVersionIsAlreadyKnown_ShouldReturnFalse()
    {
        var projectId = Guid.Parse("8d8343c2-2664-4e4a-bb80-92a7273e9cf3");

        Assert.False(ProjectReconnectVersionPolicy.ShouldCatchUp(projectId, 4, projectId, 4));
        Assert.False(ProjectReconnectVersionPolicy.ShouldCatchUp(projectId, 4, projectId, 3));
    }

    [Fact]
    [Trait("Spec", "07")]
    [Trait("Level", "Unit")]
    public void ShouldCatchUp_WhenKnownProjectDiffers_ShouldReturnTrue()
    {
        var knownProjectId = Guid.Parse("ae480458-86d8-4c09-8774-755036132e95");
        var loadedProjectId = Guid.Parse("f4fdeee8-35d9-49e0-8d83-8e2b7d3025e0");

        Assert.True(ProjectReconnectVersionPolicy.ShouldCatchUp(knownProjectId, 7, loadedProjectId, 1));
    }
}
