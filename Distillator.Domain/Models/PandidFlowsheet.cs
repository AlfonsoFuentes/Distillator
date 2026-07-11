namespace Distillator.Domain.Models;

public class PandidFlowsheet : Flowsheet
{
    public override string TypeCode => "PAndID";

    public PandidFlowsheet(string name, IFlowsheetType typeDefinition, IProject project)
        : base(name, typeDefinition, project)
    {
    }
}
