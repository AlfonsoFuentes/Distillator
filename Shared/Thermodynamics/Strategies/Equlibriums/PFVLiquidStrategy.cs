using Shared.Thermodynamics.Phases;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Equlibriums
{   // ✅ NUEVO ARCHIVO: IEquilibriumContext.cs
    // ========================================================================
    // ESTRATEGIA P_FV (P + FV → calcular T)
    // ========================================================================
    public class PFVLiquidStrategy : IEquilibriumStrategy
    {
        private readonly StreamSimulationFacade _facade;
        private MaterialStream Stream => _facade.MaterialStream;

     

        public PFVLiquidStrategy(StreamSimulationFacade facade)
        {
            _facade = facade;
        }

        public void Execute()
        {



            // ✅ 2. Preparar composición líquida (x_i = z_i para burbuja)
           

            // ✅ 3. Resolver T_bubble: encontrar T donde Σ(K_i · z_i) = 1
            double tBubble = Stream.SolveSaturationTemperature();

            var temperature = _facade.TemperatureControlled.Value;
            temperature!.SetValue(tBubble, TemperatureUnits.Kelvin);

            // ✅ 4. Actualizar UI con resultado calculado
            _facade.TemperatureControlled.SetValueCalculated(temperature,_facade.Name);
            _facade.AddCalculatedVariable(_facade.TemperatureControlled);
           
        }

      
        /// <summary>
        /// Resuelve la ecuación de punto de burbuja: Σ(K_i · z_i) = 1
        /// Usa BisectionSolver para encontrar T que satisface la ecuación.
        /// </summary>
        

        
    }
}
