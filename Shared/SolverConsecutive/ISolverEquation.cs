using Shared.SolverConsecutive.Equipments;

namespace Shared.SolverConsecutive
{
    public interface ISolverEquation
    {
        string Name { get; }
        SolverEquationType EquationType { get; }
        List<double> Residuals { get; }
        List<IVariable> Variables { get; }

        List<IVariable> AdjustableVariables() => Variables.Where(x => !x.IsDefined).ToList();



    }

}
