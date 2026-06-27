using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Equlibriums
{
    public class PFVTwoPhaseStrategy : IEquilibriumStrategy
    {
        private readonly IFacadeStream _facade;
        public PFVTwoPhaseStrategy(IFacadeStream facade) => _facade = facade;

        public void Execute()
        {
           
            // Buscamos la T que genera ese VF a la Presión actual
            _facade.MaterialStream.SolveFlashPVF();

           

            _facade.Temperature.SetValue(_facade.MaterialStream.Temperature, SolverConsecutive.VariableDefinedBy.StreamCalculated);
            _facade.MaterialStream.CalculateBulkProperties();
            _facade.MassEnthalpy.SetValue(_facade.MaterialStream.MassEnthalpy, SolverConsecutive.VariableDefinedBy.StreamCalculated);

        }
    }
   

}
