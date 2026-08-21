using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using Shared.Thermodynamics.Phases;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Equlibriums
{
    public class PFVVaporStrategy : IEquilibriumStrategy
    {
        private readonly IFacadeStream _facade;
        private IMaterialStream Stream => _facade.MaterialStream;

        // Tolerancias locales


        public PFVVaporStrategy(IFacadeStream facade)
        {
            _facade = facade;
        }

        public void Execute()
        {


            Stream.SolveSaturationTemperature();

          
            // ✅ 4. Actualizar UI con resultado calculado
            CalculatedVariableSetter.SetStreamCalculated(_facade.Temperature, Stream.Temperature);
            _facade.MaterialStream.CalculateBulkProperties();
            CalculatedVariableSetter.SetStreamCalculated(_facade.MassEnthalpy, _facade.MaterialStream.MassEnthalpy);

        }





    }
  
}
