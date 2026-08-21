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

           

            CalculatedVariableSetter.SetStreamCalculated(_facade.Temperature, _facade.MaterialStream.Temperature);
            _facade.MaterialStream.CalculateBulkProperties();
            CalculatedVariableSetter.SetStreamCalculated(_facade.MassEnthalpy, _facade.MaterialStream.MassEnthalpy);

        }
    }
   

}
