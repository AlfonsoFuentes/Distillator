using Shared.Projects;

namespace Distillator.Core.Tests.Persistence;

public sealed class ProjectRealtimeVersionPolicyTests
{
    [Fact]
    [Trait("Spec", "07")]
    [Trait("Level", "Unit")]
    public void ShouldIgnoreEvent_WhenVersionIsAlreadyKnown_ShouldReturnTrue()
    {
        var projectId = Guid.Parse("5ddc89dc-b4fa-4f89-ad09-582f7cbb62b7");

        Assert.True(ProjectRealtimeVersionPolicy.ShouldIgnoreEvent(projectId, 4, projectId, 4));
        Assert.True(ProjectRealtimeVersionPolicy.ShouldIgnoreEvent(projectId, 4, projectId, 3));
        Assert.False(ProjectRealtimeVersionPolicy.ShouldIgnoreEvent(projectId, 4, projectId, 5));
    }

    [Fact]
    [Trait("Spec", "07")]
    [Trait("Level", "Unit")]
    public void CanPublishLoadedDocument_WhenLoadedVersionCoversEventAndIsNew_ShouldReturnTrue()
    {
        var projectId = Guid.Parse("f39f8cb2-b48b-4790-a464-0680e0359db1");

        var result = ProjectRealtimeVersionPolicy.CanPublishLoadedDocument(
            projectId,
            knownVersion: 4,
            eventProjectId: projectId,
            eventVersion: 5,
            loadedProjectId: projectId,
            loadedVersion: 6);

        Assert.True(result);
    }

    [Fact]
    [Trait("Spec", "07")]
    [Trait("Level", "Unit")]
    public void CanPublishLoadedDocument_WhenLoadedVersionIsOlderThanEvent_ShouldReturnFalse()
    {
        var projectId = Guid.Parse("663c709b-c46b-4625-8ecd-cba284ef251d");

        var result = ProjectRealtimeVersionPolicy.CanPublishLoadedDocument(
            projectId,
            knownVersion: 4,
            eventProjectId: projectId,
            eventVersion: 6,
            loadedProjectId: projectId,
            loadedVersion: 5);

        Assert.False(result);
    }

    [Fact]
    [Trait("Spec", "07")]
    [Trait("Level", "Unit")]
    public void CanPublishLoadedDocument_WhenLoadedVersionIsAlreadyKnown_ShouldReturnFalse()
    {
        var projectId = Guid.Parse("3f2bb840-f458-426c-b1fb-0635fe96950a");

        var result = ProjectRealtimeVersionPolicy.CanPublishLoadedDocument(
            projectId,
            knownVersion: 7,
            eventProjectId: projectId,
            eventVersion: 6,
            loadedProjectId: projectId,
            loadedVersion: 7);

        Assert.False(result);
    }

    [Fact]
    [Trait("Spec", "07")]
    [Trait("Level", "Unit")]
    public void CanPublishLoadedDocument_WhenLoadedProjectDiffers_ShouldReturnFalse()
    {
        var eventProjectId = Guid.Parse("4370c3d1-c700-4e63-bb37-7a0c43040778");
        var loadedProjectId = Guid.Parse("b4adf51a-cadb-4500-b628-c8a5c8142c6e");

        var result = ProjectRealtimeVersionPolicy.CanPublishLoadedDocument(
            knownProjectId: null,
            knownVersion: 0,
            eventProjectId,
            eventVersion: 1,
            loadedProjectId,
            loadedVersion: 1);

        Assert.False(result);
    }
}
