namespace Shared.ProcessFlowDiagram.Designs;

public interface IDesignableEquipment
{
    IReadOnlyList<IEquipmentDesign> Designs { get; }
    IEquipmentDesign CreateDesign();
    IEquipmentDesign RecalculateDesign(IEquipmentDesign design);
}
