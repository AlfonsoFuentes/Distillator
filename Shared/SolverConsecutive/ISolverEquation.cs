using Shared.SolverConsecutive.Equipments;

namespace Shared.SolverConsecutive
{
    public interface ISolverEquation
    {
        string Name { get; }
        SolverEquationType EquationType { get; }
        List<double> Residuals { get; }
        List<INewVariable> Variables { get; }

        List<INewVariable> AdjustableVariables() => Variables.Where(x => !x.IsDefined).ToList();



    }

}
