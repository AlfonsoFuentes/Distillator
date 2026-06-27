using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Equlibriums
{
    public class TFVTwoPhaseStrategy : IEquilibriumStrategy
    {
        private readonly IFacadeStream _facade;
        public TFVTwoPhaseStrategy(IFacadeStream facade) => _facade = facade;

        public void Execute()
        {
         
            // Buscamos la P que genera ese VF a la Temperatura actual
           _facade.MaterialStream.SolveFlashTVF();

           

            _facade.Pressure.SetValue(_facade.MaterialStream.Pressure, SolverConsecutive.VariableDefinedBy.StreamCalculated);
            _facade.MaterialStream.CalculateBulkProperties();
            _facade.MassEnthalpy.SetValue(_facade.MaterialStream.MassEnthalpy, SolverConsecutive.VariableDefinedBy.StreamCalculated);
        }
    }
   
}
