using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Equlibriums
{
    public class TFVTwoPhaseStrategy : IEquilibriumStrategy
    {
        private readonly IFacadeStream _facade;
        public TFVTwoPhaseStrategy(IFacadeStream facade) => _facade = facade;

        public void Execute()
        {
         
            // Buscamos la P que genera ese VF a la Temperatura actual
           _facade.MaterialStream.SolveFlashTVF();

           

            CalculatedVariableSetter.SetStreamCalculated(_facade.Pressure, _facade.MaterialStream.Pressure);
            _facade.MaterialStream.CalculateBulkProperties();
            CalculatedVariableSetter.SetStreamCalculated(_facade.MassEnthalpy, _facade.MaterialStream.MassEnthalpy);
        }
    }
   
}
