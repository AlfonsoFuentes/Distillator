using Distillator.Domain.Inputs;

namespace Distillator.Core.Tests.Inputs;

public sealed class ProjectAuthoritativeSyncPolicyTests
{
    [Fact]
    [Trait("Spec", "07")]
    [Trait("Level", "Unit")]
    public void ShouldAttempt_WhenEverythingIsIdle_ShouldAllowSync()
    {
        var state = new ProjectAuthoritativeSyncState(
            HasActiveProject: true,
            IsHydrating: false,
            AutosaveState: AutosaveRevisionState.Clean,
            IsSimulationRunning: false,
            HasActiveVisualOperation: false);

        Assert.True(ProjectAuthoritativeSyncPolicy.ShouldAttempt(state));
    }

    [Theory]
    [Trait("Spec", "07")]
    [Trait("Level", "Unit")]
    [InlineData(false, false, AutosaveRevisionState.Clean, false, false)]
    [InlineData(true, true, AutosaveRevisionState.Clean, false, false)]
    [InlineData(true, false, AutosaveRevisionState.Dirty, false, false)]
    [InlineData(true, false, AutosaveRevisionState.Saving, false, false)]
    [InlineData(true, false, AutosaveRevisionState.Clean, true, false)]
    [InlineData(true, false, AutosaveRevisionState.Clean, false, true)]
    public void ShouldAttempt_WhenUnsafeConditionExists_ShouldDeferSync(
        bool hasActiveProject,
        bool isHydrating,
        AutosaveRevisionState autosaveState,
        bool isSimulationRunning,
        bool hasActiveVisualOperation)
    {
        var state = new ProjectAuthoritativeSyncState(
            hasActiveProject,
            isHydrating,
            autosaveState,
            isSimulationRunning,
            hasActiveVisualOperation);

        Assert.False(ProjectAuthoritativeSyncPolicy.ShouldAttempt(state));
    }
}
