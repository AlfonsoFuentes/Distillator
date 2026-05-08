using Shared.Thermodynamics.Phases;
using Shared.UnitOperations.Streams;
using UnitSystem;

namespace Shared.Thermodynamics.Strategies.Equlibriums
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
            var temperature = _facade.Temperature.Value;
            temperature!.SetValue(tBubble, TemperatureUnits.Kelvin);
            // ✅ 4. Actualizar UI con resultado calculado
            _facade.Temperature.SetValueCalculated(temperature, _facade.Name);
            

        }

       

       
        
    }
    public class PFVVaporStrategy2 : IEquilibriumStrategy
    {
        private readonly IStreamFacade _facade;
        private IMaterialStream Stream => _facade.MaterialStream;

        // Tolerancias locales


        public PFVVaporStrategy2(IStreamFacade facade)
        {
            _facade = facade;
        }

        public void Execute()
        {


            double tBubble = Stream.SolveSaturationTemperature();

            // ✅ 4. Actualizar UI con resultado calculado
            var temperature = _facade.Temperature.Value;
            temperature!.SetValue(tBubble, TemperatureUnits.Kelvin);
            // ✅ 4. Actualizar UI con resultado calculado
            _facade.Temperature.SetValueFromStream(temperature, _facade.Name);
            _facade.MaterialStream.CalculateBulkProperties();
            _facade.MolarEnthalpy.SetValueFromStream(_facade.MaterialStream.MolarEnthalpy, _facade.Name);

        }





    }
}
