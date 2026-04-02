using Shared.Thermodynamics.WaterProperties;
using UnitSystem;

namespace Shared.DesignPatterns.PureComponents.Liquido.Water
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
