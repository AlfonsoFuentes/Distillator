using Distillator.Domain.Services;

namespace Distillator.Core.Tests.Hydration;

public sealed class ProjectHydrationPublicationGateTests
{
    [Fact]
    [Trait("Spec", "03")]
    [Trait("Level", "Unit")]
    public void CanPublish_WhenRequestIsLatestAndProjectMatches_ShouldAllowPublication()
    {
        var projectId = Guid.NewGuid();
        var gate = new ProjectHydrationPublicationGate();

        var request = gate.Begin(projectId);

        Assert.True(gate.CanPublish(request, projectId));
    }

    [Fact]
    [Trait("Spec", "03")]
    [Trait("Level", "Unit")]
    public void CanPublish_WhenNewerRequestExists_ShouldRejectOlderPublication()
    {
        var olderProjectId = Guid.NewGuid();
        var newerProjectId = Guid.NewGuid();
        var gate = new ProjectHydrationPublicationGate();

        var older = gate.Begin(olderProjectId);
        gate.Begin(newerProjectId);

        Assert.False(gate.CanPublish(older, olderProjectId));
    }

    [Fact]
    [Trait("Spec", "03")]
    [Trait("Level", "Unit")]
    public void CanPublish_WhenHydratedProjectDoesNotMatchRequest_ShouldRejectPublication()
    {
        var requestedProjectId = Guid.NewGuid();
        var hydratedProjectId = Guid.NewGuid();
        var gate = new ProjectHydrationPublicationGate();

        var request = gate.Begin(requestedProjectId);

        Assert.False(gate.CanPublish(request, hydratedProjectId));
    }
}
