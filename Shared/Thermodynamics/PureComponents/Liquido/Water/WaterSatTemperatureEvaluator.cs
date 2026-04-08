using Shared.PropertiesDtos.WaterProperties;
using Shared.Thermodynamics.PureComponents;
using UnitSystem;

namespace Shared.Thermodynamics.PureComponents.Liquido.Water
{
    // Agua - sin dependencias
    public class WaterSatTemperatureEvaluator : IPropertyEvaluator<Pressure, Temperature>
    {
        public Temperature EvaluateAt(Pressure pressure)
        {
            double pBar = pressure.GetValue(PressureUnits.Bara);
            double tK = CPropiAgua.tSatW(pBar);
            return new Temperature(tK, TemperatureUnits.Kelvin);
        }
    }

}
