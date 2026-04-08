using Shared.PropertiesDtos.WaterProperties;
using Shared.Thermodynamics.PureComponents;
using UnitSystem;

namespace Shared.Thermodynamics.PureComponents.Gas.Water
{
    public class WaterGasThermalCondEvaluator : IPropertyEvaluator<Temperature, ThermalConductivity>
    {
        public ThermalConductivity EvaluateAt(Temperature temperature)
        {
            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);
            double cond = CPropiAgua.thconSatVapTW(tempK);

            return new ThermalConductivity(cond, ThermalConductivityUnits.W_m_K);
        }
    }

}
