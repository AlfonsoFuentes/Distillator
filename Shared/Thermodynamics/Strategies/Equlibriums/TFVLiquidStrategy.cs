using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using Shared.Thermodynamics.Phases;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Equlibriums
{
    public class TFVLiquidStrategy : IEquilibriumStrategy
    {
        private readonly IFacadeStream _facade;
        private IMaterialStream Stream => _facade.MaterialStream;



        public TFVLiquidStrategy(IFacadeStream facade)
        {
            _facade = facade;
        }

        public void Execute()
        {




            // ✅ 3. Resolver P_bubble: encontrar P donde Σ(K_i · z_i) = 1
            Stream.SolveSaturationPressure();

           
            // ✅ 4. Actualizar UI con resultado calculado
            CalculatedVariableSetter.SetStreamCalculated(_facade.Pressure, _facade.MaterialStream.Pressure);
            _facade.MaterialStream.CalculateBulkProperties();
            CalculatedVariableSetter.SetStreamCalculated(_facade.MassEnthalpy, _facade.MaterialStream.MassEnthalpy);
        }




    }
  


}
