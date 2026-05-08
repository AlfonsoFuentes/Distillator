using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Equlibriums
{   // ✅ NUEVO ARCHIVO: IEquilibriumContext.cs
    // --- ESTRATEGIA T-VF PARA ZONA BIFÁSICA (0 < VF < 1) ---
    public class TFVTwoPhaseStrategy : IEquilibriumStrategy
    {
        private readonly StreamSimulationFacade _facade;
        public TFVTwoPhaseStrategy(StreamSimulationFacade facade) => _facade = facade;

        public void Execute()
        {
            double targetVF = _facade.VaporFraction.Value;
            // Buscamos la P que genera ese VF a la Temperatura actual
            double pFound = _facade.MaterialStream.SolveFlashTVF(_facade.MaterialStream.Temperature, targetVF);

            var pres = _facade.Pressure.Value!;
            pres.SetValue(pFound, PressureUnits.KiloPascala);

            _facade.Pressure.SetValueCalculated(pres, _facade.Name);
         
        }
    }
    public class TFVTwoPhaseStrategy2 : IEquilibriumStrategy
    {
        private readonly IStreamFacade _facade;
        public TFVTwoPhaseStrategy2(IStreamFacade facade) => _facade = facade;

        public void Execute()
        {
            double targetVF = _facade.VaporFraction.Value;
            // Buscamos la P que genera ese VF a la Temperatura actual
            double pFound = _facade.MaterialStream.SolveFlashTVF(_facade.Temperature.Value, targetVF);

            var pres = _facade.Pressure.Value!;
            pres.SetValue(pFound, PressureUnits.KiloPascala);

            _facade.Pressure.SetValueFromStream(pres, _facade.Name);
            _facade.MaterialStream.CalculateBulkProperties();
            _facade.MolarEnthalpy.SetValueFromStream(_facade.MaterialStream.MolarEnthalpy, _facade.Name);
        }
    }
}
