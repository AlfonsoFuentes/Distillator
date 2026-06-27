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
            _facade.Temperature.SetValue(Stream.Temperature, SolverConsecutive.VariableDefinedBy.StreamCalculated);
            _facade.MaterialStream.CalculateBulkProperties();
            _facade.MassEnthalpy.SetValue(_facade.MaterialStream.MassEnthalpy, SolverConsecutive.VariableDefinedBy.StreamCalculated);

        }





    }
  
}
