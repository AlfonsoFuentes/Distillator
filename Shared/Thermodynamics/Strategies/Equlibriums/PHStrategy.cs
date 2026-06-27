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
            _facade.Temperature.SetValue(_facade.MaterialStream.Temperature, SolverConsecutive.VariableDefinedBy.StreamCalculated);

            _facade.VaporFraction.SetValue(_facade.MaterialStream.VaporFraction, SolverConsecutive.VariableDefinedBy.StreamCalculated);
            _facade.MaterialStream.CalculateBulkProperties();

        }
    }
   
}
