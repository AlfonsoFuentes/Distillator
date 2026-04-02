using Shared.DesignPatterns.Thermodynamics.Phases;
using UnitSystem;

namespace Shared.DesignPatterns.Thermodynamics.Strategies.Equlibriums
{   // ✅ NUEVO ARCHIVO: IEquilibriumContext.cs
    // --- ESTRATEGIA P-VF PARA ZONA BIFÁSICA (0 < VF < 1) ---
    public class PFVTwoPhaseStrategy : IEquilibriumStrategy
    {
        private readonly StreamSimulationFacade _facade;
        public PFVTwoPhaseStrategy(StreamSimulationFacade facade) => _facade = facade;

        public void Execute()
        {
            double targetVF = _facade.VaporFractionControlled.Value;
            // Buscamos la T que genera ese VF a la Presión actual
            double tFound = _facade.MaterialStream.SolveFlashPVF(_facade.MaterialStream.Pressure, targetVF);

            var temp = _facade.TemperatureControlled.Value!;
            temp.SetValue(tFound, TemperatureUnits.Kelvin);

            _facade.TemperatureControlled.SetValueCalculated(temp, _facade.Name);
            _facade.AddCalculatedVariable(_facade.TemperatureControlled);
        }
    }
}
