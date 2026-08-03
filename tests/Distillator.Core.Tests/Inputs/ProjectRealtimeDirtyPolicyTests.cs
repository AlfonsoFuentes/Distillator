using Distillator.Domain.Inputs;

namespace Distillator.Core.Tests.Inputs;

public sealed class ProjectRealtimeDirtyPolicyTests
{
    [Theory]
    [Trait("Spec", "07")]
    [Trait("Level", "Unit")]
    [InlineData(AutosaveRevisionState.Clean, false)]
    [InlineData(AutosaveRevisionState.Dirty, true)]
    [InlineData(AutosaveRevisionState.Saving, true)]
    public void ShouldDeferRemoteReload_ShouldDeferOnlyWhenLocalAutosaveIsNotClean(
        AutosaveRevisionState autosaveState,
        bool expected)
    {
        Assert.Equal(expected, ProjectRealtimeDirtyPolicy.ShouldDeferRemoteReload(autosaveState));
    }
}
