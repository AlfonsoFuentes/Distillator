namespace Distillator.Domain.Policies;

public enum FlowsheetEditChangeKind
{
    Visual,
    Topological
}

public sealed class FlowsheetEditChangePolicy
{
    public bool ShouldRunSimulation(FlowsheetEditChangeKind changeKind)
    {
        return changeKind == FlowsheetEditChangeKind.Topological;
    }

    public bool ShouldPersistVisualState(FlowsheetEditChangeKind changeKind)
    {
        return changeKind == FlowsheetEditChangeKind.Visual ||
               changeKind == FlowsheetEditChangeKind.Topological;
    }
}
