using UnitSystem;

namespace Shared.DesignPatterns.Thermodynamics.Strategies.Equlibriums
{   // ✅ NUEVO ARCHIVO: IEquilibriumContext.cs
    // --- ESTRATEGIA T-VF PARA ZONA BIFÁSICA (0 < VF < 1) ---
    public class TFVTwoPhaseStrategy : IEquilibriumStrategy
    {
        private readonly StreamSimulationFacade _facade;
        public TFVTwoPhaseStrategy(StreamSimulationFacade facade) => _facade = facade;

        public void Execute()
        {
            double targetVF = _facade.VaporFractionControlled.Value;
            // Buscamos la P que genera ese VF a la Temperatura actual
            double pFound = _facade.MaterialStream.SolveFlashTVF(_facade.MaterialStream.Temperature, targetVF);

            var pres = _facade.PressureControlled.Value!;
            pres.SetValue(pFound, PressureUnits.KiloPascal);

            _facade.PressureControlled.SetValueCalculated(pres, _facade.Name);
            _facade.AddCalculatedVariable(_facade.PressureControlled);
        }
    }
}
