using Shared.DesignPatterns.Thermodynamics.Phases;

namespace Shared.DesignPatterns.Thermodynamics.Strategies.Equlibriums
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

           
            _facade.VaporFractionControlled.SetValueCalculated(Stream.VaporFraction, _facade.Name);

            _facade.AddCalculatedVariable(_facade.VaporFractionControlled);

            
        }
    }
}
