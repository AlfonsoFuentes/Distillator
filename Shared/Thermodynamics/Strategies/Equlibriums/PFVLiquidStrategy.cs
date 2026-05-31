using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using Shared.Thermodynamics.Phases;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Equlibriums
{
    public class PFVLiquidStrategy : IEquilibriumStrategy
    {
        private readonly IFacadeStream _facade;
        private IMaterialStream Stream => _facade.MaterialStream;



        public PFVLiquidStrategy(IFacadeStream facade)
        {
            _facade = facade;
        }

        public void Execute()
        {



            // ✅ 2. Preparar composición líquida (x_i = z_i para burbuja)


            // ✅ 3. Resolver T_bubble: encontrar T donde Σ(K_i · z_i) = 1
            Stream.SolveSaturationTemperature();

            // ✅ 4. Actualizar UI con resultado calculado
            _facade.Temperature.SetValue(Stream.Temperature, VariableDataProcedence.StreamCalculated);
            Stream.CalculateBulkProperties();
            _facade.MassEnthalpy.SetValue(Stream.MassEnthalpy, VariableDataProcedence.StreamCalculated);

        }


        /// <summary>
        /// Resuelve la ecuación de punto de burbuja: Σ(K_i · z_i) = 1
        /// Usa BisectionSolver para encontrar T que satisface la ecuación.
        /// </summary>



    }
    public class PFVLiquidStrategy3 : IEquilibriumStrategy
    {
        private readonly IStreamFacade _facade;
        private IMaterialStream Stream => _facade.MaterialStream;



        public PFVLiquidStrategy3(IStreamFacade facade)
        {
            _facade = facade;
        }

        public void Execute()
        {



            // ✅ 2. Preparar composición líquida (x_i = z_i para burbuja)


            // ✅ 3. Resolver T_bubble: encontrar T donde Σ(K_i · z_i) = 1
            //double tBubble = Stream.SolveSaturationTemperature();

            //var temperature = _facade.Temperature.Value;
            //temperature!.SetValue(tBubble, TemperatureUnits.Kelvin);

            //// ✅ 4. Actualizar UI con resultado calculado
            //_facade.Temperature.SetValueFromStream(temperature, _facade.Name);
            //Stream.CalculateBulkProperties();
            //_facade.MassEnthalpy.SetValueFromStream(Stream.MassEnthalpy, _facade.Name);

        }


        /// <summary>
        /// Resuelve la ecuación de punto de burbuja: Σ(K_i · z_i) = 1
        /// Usa BisectionSolver para encontrar T que satisface la ecuación.
        /// </summary>



    }

    public class PFVLiquidStrategy2 : IEquilibriumStrategy
    {
        private readonly IStreamFacade2 _facade;
        private IMaterialStream Stream => _facade.MaterialStream;



        public PFVLiquidStrategy2(IStreamFacade2 facade)
        {
            _facade = facade;
        }

        public void Execute()
        {



            //// ✅ 2. Preparar composición líquida (x_i = z_i para burbuja)


            //// ✅ 3. Resolver T_bubble: encontrar T donde Σ(K_i · z_i) = 1
            //double tBubble = Stream.SolveSaturationTemperature();

            //var temperature = _facade.Temperature.Value;
            //temperature!.SetValue(tBubble, TemperatureUnits.Kelvin);

            //// ✅ 4. Actualizar UI con resultado calculado
            //_facade.Temperature.SetValueFromStream(temperature, _facade.Name);
            //Stream.CalculateBulkProperties();
            //_facade.MassEnthalpy.SetValueFromStream(Stream.MassEnthalpy, _facade.Name);

        }


        /// <summary>
        /// Resuelve la ecuación de punto de burbuja: Σ(K_i · z_i) = 1
        /// Usa BisectionSolver para encontrar T que satisface la ecuación.
        /// </summary>



    }
}
