using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using Shared.Thermodynamics.Phases;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Equlibriums
{
    public class PTStrategy : IEquilibriumStrategy
    {
        private readonly IFacadeStream _facade;
        private IMaterialStream Stream => _facade.MaterialStream;

        public PTStrategy(IFacadeStream facade)
        {
            _facade = facade;
        }

        public void Execute()
        {

            Stream.PerformFlashPT();


            CalculatedVariableSetter.SetStreamCalculated(_facade.VaporFraction, Stream.VaporFraction);

            _facade.MaterialStream.CalculateBulkProperties();
            CalculatedVariableSetter.SetStreamCalculated(_facade.MassEnthalpy, _facade.MaterialStream.MassEnthalpy);

        }
    }
   

}
