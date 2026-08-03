using Distillator.Domain.Policies;

namespace Distillator.Core.Tests.Topology;

public sealed class FlowsheetEditChangePolicyTests
{
    [Theory]
    [Trait("Spec", "04")]
    [Trait("Level", "Unit")]
    [InlineData(FlowsheetEditChangeKind.Visual, false)]
    [InlineData(FlowsheetEditChangeKind.Topological, true)]
    public void ShouldRunSimulation_ShouldOnlyRunForTopologicalChanges(
        FlowsheetEditChangeKind changeKind,
        bool expected)
    {
        var policy = new FlowsheetEditChangePolicy();

        Assert.Equal(expected, policy.ShouldRunSimulation(changeKind));
    }

    [Theory]
    [Trait("Spec", "04")]
    [Trait("Level", "Unit")]
    [InlineData(FlowsheetEditChangeKind.Visual)]
    [InlineData(FlowsheetEditChangeKind.Topological)]
    public void ShouldPersistVisualState_ShouldPersistAcceptedVisualAndTopologicalChanges(
        FlowsheetEditChangeKind changeKind)
    {
        var policy = new FlowsheetEditChangePolicy();

        Assert.True(policy.ShouldPersistVisualState(changeKind));
    }
}
