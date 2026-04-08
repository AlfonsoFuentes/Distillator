using Shared.Thermodynamics.Phases;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Equlibriums
{   // ✅ NUEVO ARCHIVO: IEquilibriumContext.cs
    public class TFVVaporStrategy : IEquilibriumStrategy
    {
        private readonly StreamSimulationFacade _facade;
        private MaterialStream Stream => _facade.MaterialStream;

     

        public TFVVaporStrategy(StreamSimulationFacade facade)
        {
            _facade = facade;
        }

        public void Execute()
        {
            double pBubble = Stream.SolveSaturationPressure();

            var pressure = _facade.PressureControlled.Value;
            pressure!.SetValue(pBubble, PressureUnits.KiloPascala);
            // ✅ 4. Actualizar UI con resultado calculado
            _facade.PressureControlled.SetValueCalculated(pressure, _facade.Name);
            _facade.AddCalculatedVariable(_facade.PressureControlled);

        }

      

       

    }
}
