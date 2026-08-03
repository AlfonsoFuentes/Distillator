using Shared.Projects;

namespace Distillator.Core.Tests.Persistence;

public sealed class ProjectOperationIdTests
{
    [Fact]
    [Trait("Spec", "06")]
    [Trait("Level", "Unit")]
    public void IsSpecified_ShouldRejectMissingAndEmptyOperationIds()
    {
        Assert.False(ProjectOperationId.IsSpecified(null));
        Assert.False(ProjectOperationId.IsSpecified(Guid.Empty));
        Assert.True(ProjectOperationId.IsSpecified(Guid.Parse("32064155-e756-40f7-ab48-1b7c47264f44")));
    }

    [Fact]
    [Trait("Spec", "06")]
    [Trait("Level", "Unit")]
    public void Normalize_ShouldReturnNullForMissingAndEmptyOperationIds()
    {
        var operationId = Guid.Parse("88af5efa-70fd-43af-94c2-46f847c87d36");

        Assert.Null(ProjectOperationId.Normalize(null));
        Assert.Null(ProjectOperationId.Normalize(Guid.Empty));
        Assert.Equal(operationId, ProjectOperationId.Normalize(operationId));
    }
}
