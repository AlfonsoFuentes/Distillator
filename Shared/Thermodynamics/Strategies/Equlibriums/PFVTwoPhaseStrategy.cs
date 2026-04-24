using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Equlibriums
{   // ✅ NUEVO ARCHIVO: IEquilibriumContext.cs
    // --- ESTRATEGIA P-VF PARA ZONA BIFÁSICA (0 < VF < 1) ---
    public class PFVTwoPhaseStrategy : IEquilibriumStrategy
    {
        private readonly StreamSimulationFacade _facade;
        public PFVTwoPhaseStrategy(StreamSimulationFacade facade) => _facade = facade;

        public void Execute()
        {
            double targetVF = _facade.VaporFraction.Value;
            // Buscamos la T que genera ese VF a la Presión actual
            double tFound = _facade.MaterialStream.SolveFlashPVF(_facade.MaterialStream.Pressure, targetVF);

            var temp = _facade.Temperature.Value!;
            temp.SetValue(tFound, TemperatureUnits.Kelvin);

            _facade.Temperature.SetValueCalculated(temp, _facade.Name);
         
        }
    }
}
