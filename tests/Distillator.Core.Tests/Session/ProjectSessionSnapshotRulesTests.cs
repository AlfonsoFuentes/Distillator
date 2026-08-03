using Distillator.Domain.Session;

namespace Distillator.Core.Tests.Session;

public sealed class ProjectSessionSnapshotRulesTests
{
    [Fact]
    [Trait("Spec", "08")]
    [Trait("Level", "Unit")]
    public void Clear_WhenSessionHasProjectDiagramAndPresence_ShouldRemoveSensitiveState()
    {
        var snapshot = new ProjectSessionSnapshot(
            ProjectId: Guid.NewGuid(),
            FlowsheetId: Guid.NewGuid(),
            HasPresence: true);

        var cleared = ProjectSessionSnapshotRules.Clear(snapshot);

        Assert.False(cleared.HasActiveProject);
        Assert.Null(cleared.ProjectId);
        Assert.Null(cleared.FlowsheetId);
        Assert.False(cleared.HasPresence);
    }

    [Fact]
    [Trait("Spec", "08")]
    [Trait("Level", "Unit")]
    public void Clear_WhenSessionIsAlreadyEmpty_ShouldRemainEmpty()
    {
        var snapshot = new ProjectSessionSnapshot(null, null, HasPresence: false);

        var cleared = ProjectSessionSnapshotRules.Clear(snapshot);

        Assert.False(cleared.HasActiveProject);
        Assert.Null(cleared.ProjectId);
        Assert.Null(cleared.FlowsheetId);
        Assert.False(cleared.HasPresence);
    }
}
