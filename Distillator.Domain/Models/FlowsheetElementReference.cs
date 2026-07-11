namespace Distillator.Domain.Models;

public class FlowsheetElementReference : IFlowsheetElementReference
{
    public Guid ElementId { get; }
    public double X { get; set; }
    public double Y { get; set; }
    public int RotationAngle { get; set; }
    public int ZIndex { get; set; }
    public bool IsFlippedHorizontal { get; set; }
    public bool IsFlippedVertical { get; set; }

    public FlowsheetElementReference(Guid elementId, double x, double y)
    {
        ElementId = elementId;
        X = x;
        Y = y;
    }
}
