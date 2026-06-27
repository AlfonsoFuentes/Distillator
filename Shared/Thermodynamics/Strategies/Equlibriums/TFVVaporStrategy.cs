using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using Shared.Thermodynamics.Phases;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Equlibriums
{
    public class TFVVaporStrategy : IEquilibriumStrategy
    {
        private readonly IFacadeStream _facade;
        private IMaterialStream Stream => _facade.MaterialStream;



        public TFVVaporStrategy(IFacadeStream   facade)
        {
            _facade = facade;
        }

        public void Execute()
        {
            Stream.SolveSaturationPressure();

        
            // ✅ 4. Actualizar UI con resultado calculado
            _facade.Pressure.SetValue(Stream.Pressure, SolverConsecutive.VariableDefinedBy.StreamCalculated);

            _facade.MaterialStream.CalculateBulkProperties();
            _facade.MassEnthalpy.SetValue(_facade.MaterialStream.MassEnthalpy, SolverConsecutive.VariableDefinedBy.StreamCalculated);
        }





    }
   

}
