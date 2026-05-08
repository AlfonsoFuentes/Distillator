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

            var temperature = _facade.Temperature.Value;
            temperature!.SetValue(tBubble, TemperatureUnits.Kelvin);

            // ✅ 4. Actualizar UI con resultado calculado
            _facade.Temperature.SetValueCalculated(temperature, _facade.Name);




        }


        /// <summary>
        /// Resuelve la ecuación de punto de burbuja: Σ(K_i · z_i) = 1
        /// Usa BisectionSolver para encontrar T que satisface la ecuación.
        /// </summary>



    }

    public class PFVLiquidStrategy2 : IEquilibriumStrategy
    {
        private readonly IStreamFacade _facade;
        private IMaterialStream Stream => _facade.MaterialStream;



        public PFVLiquidStrategy2(IStreamFacade facade)
        {
            _facade = facade;
        }

        public void Execute()
        {



            // ✅ 2. Preparar composición líquida (x_i = z_i para burbuja)


            // ✅ 3. Resolver T_bubble: encontrar T donde Σ(K_i · z_i) = 1
            double tBubble = Stream.SolveSaturationTemperature();

            var temperature = _facade.Temperature.Value;
            temperature!.SetValue(tBubble, TemperatureUnits.Kelvin);

            // ✅ 4. Actualizar UI con resultado calculado
            _facade.Temperature.SetValueFromStream(temperature, _facade.Name);
            Stream.CalculateBulkProperties();
            _facade.MolarEnthalpy.SetValueFromStream(Stream.MolarEnthalpy, _facade.Name);

        }


        /// <summary>
        /// Resuelve la ecuación de punto de burbuja: Σ(K_i · z_i) = 1
        /// Usa BisectionSolver para encontrar T que satisface la ecuación.
        /// </summary>



    }
}
