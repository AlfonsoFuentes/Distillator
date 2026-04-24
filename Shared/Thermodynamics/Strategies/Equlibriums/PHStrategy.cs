using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Equlibriums
{   // ✅ NUEVO ARCHIVO: IEquilibriumContext.cs
    public class PHStrategy : IEquilibriumStrategy
    {
        private readonly StreamSimulationFacade _facade;
        public PHStrategy(StreamSimulationFacade facade) => _facade = facade;

        public void Execute()
        {
            var pres = _facade.Pressure.Value!;
            var enth = _facade.MolarEnthalpy.Value!; // Asume que inyectaste la entalpía objetivo aquí

            // 1. Ejecutar Flash Riguroso
            _facade.MaterialStream.PerformFlashPH(pres, enth);

            // 2. Extraer T y VF calculados
            var temp = _facade.Temperature.Value ?? new Temperature(0, TemperatureUnits.Kelvin);
            temp.SetValue(_facade.MaterialStream.Temperature.GetValue(TemperatureUnits.Kelvin), TemperatureUnits.Kelvin);

            double calculatedVF = _facade.MaterialStream.VaporFraction;

            // 3. Registrar en Facade
            _facade.Temperature.SetValueCalculated(temp, _facade.Name);
            _facade.VaporFraction.SetValueCalculated(calculatedVF, _facade.Name);

         
        }
    }
}
