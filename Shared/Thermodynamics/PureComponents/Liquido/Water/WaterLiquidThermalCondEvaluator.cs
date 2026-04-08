using Shared.PropertiesDtos.WaterProperties;
using Shared.Thermodynamics.PureComponents;
using UnitSystem;

namespace Shared.Thermodynamics.PureComponents.Liquido.Water
{
    public class WaterLiquidThermalCondEvaluator : IPropertyEvaluator<Temperature, ThermalConductivity>
    {
        public ThermalConductivity EvaluateAt(Temperature temperature)
        {
            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);
            double k = CPropiAgua.thconSatLiqTW(tempK);

            return new ThermalConductivity(k, ThermalConductivityUnits.W_m_K);
        }
    }

}
