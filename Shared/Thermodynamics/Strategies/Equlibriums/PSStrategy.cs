using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Equlibriums
{   // ✅ NUEVO ARCHIVO: IEquilibriumContext.cs
    public class PSStrategy : IEquilibriumStrategy
    {
        private readonly StreamSimulationFacade _facade;
        public PSStrategy(StreamSimulationFacade facade) => _facade = facade;

        public void Execute()
        {
            //var pres = _facade.Pressure.Value!;
            //// NOTA: Debes agregar la propiedad MolarEntropy en StreamSimulationFacade si no la tienes
            //var entr = _facade.MolarEntropy.Value!;

            //_facade.MaterialStream.PerformFlashPS(pres, entr);

            //var temp = _facade.Temperature.Value ?? new Temperature(0, TemperatureUnits.Kelvin);
            //temp.SetValue(_facade.MaterialStream.Temperature.GetValue(TemperatureUnits.Kelvin), TemperatureUnits.Kelvin);

            //double calculatedVF = _facade.MaterialStream.VaporFraction;

            //_facade.Temperature.SetValueCalculated(temp, _facade.Name);
            //_facade.VaporFractionControlled.SetValueCalculated(calculatedVF, _facade.Name);

        }
    }
}
