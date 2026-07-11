namespace Distillator.Domain.Models;

public class PfdFlowsheet : Flowsheet
{
    public override string TypeCode => "PFD";

    public PfdFlowsheet(string name, IFlowsheetType typeDefinition, IProject project)
        : base(name, typeDefinition, project)
    {
    }
}
