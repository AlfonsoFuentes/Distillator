namespace Shared.ProcessFlowDiagram.Designs;

public interface IEquipmentDesign
{
    Guid Id { get; }
    string Name { get; }
    IDesignVariables Variables { get; }
    IDesignResult Result { get; }
}
