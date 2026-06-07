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
            _facade.Temperature.SetValue(Stream.Temperature, SolverConsecutive.VariableDefinedBy.StreamCalculated);
            Stream.CalculateBulkProperties();
            _facade.MassEnthalpy.SetValue(Stream.MassEnthalpy, SolverConsecutive.VariableDefinedBy.StreamCalculated);

        }


        /// <summary>
        /// Resuelve la ecuación de punto de burbuja: Σ(K_i · z_i) = 1
        /// Usa BisectionSolver para encontrar T que satisface la ecuación.
        /// </summary>



    }
  
}
