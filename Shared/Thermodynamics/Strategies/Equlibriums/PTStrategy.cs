using Shared.Thermodynamics.Phases;
using Shared.UnitOperations.Streams;

namespace Shared.Thermodynamics.Strategies.Equlibriums
{   // ✅ NUEVO ARCHIVO: IEquilibriumContext.cs
    // ========================================================================
    // ESTRATEGIA PT (T + P → calcular FV)
    // ========================================================================
    public class PTStrategy : IEquilibriumStrategy
    {
        private readonly StreamSimulationFacade _facade;
        private MaterialStream Stream => _facade.MaterialStream;

        public PTStrategy(StreamSimulationFacade facade)
        {
            _facade = facade;
        }

        public void Execute()
        {
            
            Stream.PerformFlashPT();

           
            _facade.VaporFraction.SetValueCalculated(Stream.VaporFraction, _facade.Name);

      

            
        }
    }
    public class PTStrategy2 : IEquilibriumStrategy
    {
        private readonly IStreamFacade _facade;
        private IMaterialStream Stream => _facade.MaterialStream;

        public PTStrategy2(IStreamFacade facade)
        {
            _facade = facade;
        }

        public void Execute()
        {

            Stream.PerformFlashPT();


            _facade.VaporFraction.SetValueFromStream(Stream.VaporFraction, _facade.Name);

            _facade.MaterialStream.CalculateBulkProperties();
            _facade.MolarEnthalpy.SetValueFromStream(_facade.MaterialStream.MolarEnthalpy, _facade.Name);

        }
    }

}
