namespace Distillator.Domain.Models;

public class ElectricalFlowsheet : Flowsheet
{
    public override string TypeCode => "Electrical";

    public ElectricalFlowsheet(string name, IFlowsheetType typeDefinition, IProject project, Guid? id = null)
        : base(name, typeDefinition, project, id)
    {
    }
}
