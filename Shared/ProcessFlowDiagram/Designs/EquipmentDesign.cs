namespace Shared.ProcessFlowDiagram.Designs;

public sealed record EquipmentDesign : IEquipmentDesign
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required IDesignVariables Variables { get; init; }
    public required IDesignResult Result { get; init; }
}
