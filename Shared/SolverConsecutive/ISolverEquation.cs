using Shared.SolverConsecutive.Equipments;
using Shared.SolverQwen.Stream;

namespace Shared.SolverConsecutive
{
    public interface ISolverEquation
    {
        string Name { get; }
        SolverEquationType EquationType { get; }
        List<double> Residuals { get; }
        List<IVariable> Variables { get; }

        List<IVariable> AdjustableVariables() => Variables.Where(x => !x.IsDefined).ToList();

        SolverEquationTypeModifier EquationTypeModifer { get; }
        void RefreshEquation() { }


    }
    public interface ISpecSolverEquation : ISolverEquation
    {
        IEnumerable<IFacadeStream> AsociatedStreams { get; }
    }

}
