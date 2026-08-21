using Shared.ProcessFlowDiagram.Designs;

namespace Shared.UnitOperations.HeatExchangers.Design;

public sealed record HeatExchangerDesignResult : IDesignResult
{
    public required string DesignType { get; init; }

    public ShellAndTubeCalculationStandard CalculationStandard { get; init; } = ShellAndTubeCalculationStandard.Kern;

    public string Message { get; init; } = string.Empty;

    public IReadOnlyList<string> RequiredMethodImplementations { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Recommendations { get; init; } = Array.Empty<string>();
}
