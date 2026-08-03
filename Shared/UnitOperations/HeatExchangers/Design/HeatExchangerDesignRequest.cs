using Shared.ProcessFlowDiagram;
using Shared.SolverQwen.Stream;

namespace Shared.UnitOperations.HeatExchangers.Design;

public sealed record HeatExchangerDesignRequest
{
    public required HeatExchangerType HeatExchangerType { get; init; }
    public required ShellAndTubeDesignVariables Variables { get; init; }
    public required HeatExchangerStreamSnapshot ShellSideInlet { get; init; }
    public required HeatExchangerStreamSnapshot ShellSideOutlet { get; init; }
    public required HeatExchangerStreamSnapshot TubeSideInlet { get; init; }
    public required HeatExchangerStreamSnapshot TubeSideOutlet { get; init; }
    public IVisualElement? Equipment { get; init; }
}

public sealed record HeatExchangerStreamSnapshot
{
    public required IFacadeStream Stream { get; init; }
}
