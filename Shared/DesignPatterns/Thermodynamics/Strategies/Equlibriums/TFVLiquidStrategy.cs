using Shared.DesignPatterns.Thermodynamics.Phases;
using UnitSystem;

namespace Shared.DesignPatterns.Thermodynamics.Strategies.Equlibriums
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

            var pressure = _facade.PressureControlled.Value;
            pressure!.SetValue(pBubble, PressureUnits.KiloPascal);
            // ✅ 4. Actualizar UI con resultado calculado
            _facade.PressureControlled.SetValueCalculated(pressure,_facade.Name);
            _facade.AddCalculatedVariable(_facade.PressureControlled);

        }


      

    }
}
