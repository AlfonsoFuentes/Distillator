using Shared.DesignPatterns.Thermodynamics.Phases;
using UnitSystem;

namespace Shared.DesignPatterns.Thermodynamics.Strategies.Equlibriums
{   // ✅ NUEVO ARCHIVO: IEquilibriumContext.cs
    public class PFVVaporStrategy : IEquilibriumStrategy
    {
        private readonly StreamSimulationFacade _facade;
        private MaterialStream Stream => _facade.MaterialStream;

        // Tolerancias locales
    

        public PFVVaporStrategy(StreamSimulationFacade facade)
        {
            _facade = facade;
        }

        public void Execute()
        {


            double tBubble = Stream.SolveSaturationTemperature();

            // ✅ 4. Actualizar UI con resultado calculado
            var temperature = _facade.TemperatureControlled.Value;
            temperature!.SetValue(tBubble, TemperatureUnits.Kelvin);
            // ✅ 4. Actualizar UI con resultado calculado
            _facade.TemperatureControlled.SetValueCalculated(temperature, _facade.Name);
            _facade.AddCalculatedVariable(_facade.TemperatureControlled);

        }

       

       
        
    }
}
