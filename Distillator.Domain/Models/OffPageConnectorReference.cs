namespace Distillator.Domain.Models;

public class OffPageConnectorReference : FlowsheetElementReference, IOffPageConnectorReference
{
    public Guid? TargetFlowsheetId { get; set; }
    public Guid? TargetConnectorId { get; set; }
    public string TargetFlowsheetName { get; set; } = string.Empty;
    public string ConnectedEquipmentName { get; set; } = string.Empty;
    public bool IsOutlet { get; set; }

    public OffPageConnectorReference(Guid elementId, double x, double y, bool isOutlet = true)
        : base(elementId, x, y)
    {
        IsOutlet = isOutlet;
    }
}
