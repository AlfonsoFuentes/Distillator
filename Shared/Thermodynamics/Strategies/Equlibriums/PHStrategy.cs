using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using Shared.UnitOperations.Streams;

namespace Shared.Thermodynamics.Strategies.Equlibriums
{
    public class PHStrategy : IEquilibriumStrategy
    {
        private readonly IFacadeStream _facade;
        public PHStrategy(IFacadeStream facade) => _facade = facade;

        public void Execute()
        {
          

            // 1. Ejecutar Flash Riguroso
            _facade.MaterialStream.PerformFlashPH();

           

            // 3. Registrar en Facade
            CalculatedVariableSetter.SetStreamCalculated(_facade.Temperature, _facade.MaterialStream.Temperature);

            CalculatedVariableSetter.SetStreamCalculated(_facade.VaporFraction, _facade.MaterialStream.VaporFraction);
            _facade.MaterialStream.CalculateBulkProperties();

        }
    }
   
}
