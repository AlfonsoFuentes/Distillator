namespace Distillator.Domain.Models;

public class PandidFlowsheet : Flowsheet
{
    public override string TypeCode => "PAndID";

    public PandidFlowsheet(string name, IFlowsheetType typeDefinition, IProject project, Guid? id = null)
        : base(name, typeDefinition, project, id)
    {
    }
}
