using Shared.MatrixSolvers;
using Shared.Thermodynamics.ControlledVariables;

namespace Shared.UnitOperations.Basiss
{
    public interface ISolverEquationsProvider
    {
        EquationSystem GetEquationConcentration();
        EquationSystem GetEquationPressure();
        EquationSystem GetEquationSystem();
     
       

       
    }

}
