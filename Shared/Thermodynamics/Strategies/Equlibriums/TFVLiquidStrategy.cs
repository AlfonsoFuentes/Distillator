using Shared.Thermodynamics.Phases;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Equlibriums
{   // ✅ NUEVO ARCHIVO: IEquilibriumContext.cs
    // ========================================================================
    // ESTRATEGIA T_FV (T + FV → calcular P)
    // ========================================================================
    public class TFVLiquidStrategy : IEquilibriumStrategy
    {
        private readonly StreamSimulationFacade _facade;
        private MaterialStream Stream => _facade.MaterialStream;



        public TFVLiquidStrategy(StreamSimulationFacade facade)
        {
            _facade = facade;
        }

        public void Execute()
        {


           
          
            // ✅ 3. Resolver P_bubble: encontrar P donde Σ(K_i · z_i) = 1
            double pBubble = Stream.SolveSaturationPressure();

            var pressure = _facade.Pressure.Value;
            pressure!.SetValue(pBubble, PressureUnits.KiloPascala);
            // ✅ 4. Actualizar UI con resultado calculado
            _facade.Pressure.SetValueCalculated(pressure,_facade.Name);
          

        }


      

    }
    public class TFVLiquidStrategy2 : IEquilibriumStrategy
    {
        private readonly IStreamFacade _facade;
        private IMaterialStream Stream => _facade.MaterialStream;



        public TFVLiquidStrategy2(IStreamFacade facade)
        {
            _facade = facade;
        }

        public void Execute()
        {




            // ✅ 3. Resolver P_bubble: encontrar P donde Σ(K_i · z_i) = 1
            double pBubble = Stream.SolveSaturationPressure();

            var pressure = _facade.Pressure.Value;
            pressure!.SetValue(pBubble, PressureUnits.KiloPascala);
            // ✅ 4. Actualizar UI con resultado calculado
            _facade.Pressure.SetValueFromStream(pressure, _facade.Name);
            _facade.MaterialStream.CalculateBulkProperties();
            _facade.MolarEnthalpy.SetValueFromStream(_facade.MaterialStream.MolarEnthalpy, _facade.Name);
        }




    }


}
