namespace Distillator.Domain.Inputs;

public static class ProjectRealtimeDirtyPolicy
{
    public static bool ShouldDeferRemoteReload(AutosaveRevisionState autosaveState)
    {
        return autosaveState is AutosaveRevisionState.Dirty or AutosaveRevisionState.Saving;
    }
}
