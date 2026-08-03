using Shared.PropertiesDtos.Components;
using Shared.Thermodynamics.PureComponents;
using UnitSystem;

namespace Shared.Thermodynamics.PureComponents.Liquido.Others
{
    public class Ppds8LiquidThermalCondEvaluator : IPropertyEvaluator<Temperature, ThermalConductivity>
    {
        private readonly CorrelationCoefficientsDto _coeffs;
        private readonly Temperature _criticalTemperature;

        public Ppds8LiquidThermalCondEvaluator(
            CorrelationCoefficientsDto coeffs,
            Temperature criticalTemperature)
        {
            _coeffs = coeffs;
            _criticalTemperature = criticalTemperature;
        }

        public ThermalConductivity EvaluateAt(Temperature temperature)
        {
            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);
            double tMin = _coeffs.Tmin.GetValue(TemperatureUnits.Kelvin);
            double tMax = _coeffs.Tmax.GetValue(TemperatureUnits.Kelvin);
            double criticalTempK = _criticalTemperature.GetValue(TemperatureUnits.Kelvin);

            if (tempK < tMin || tempK > tMax || tempK >= criticalTempK)
                return new ThermalConductivity(0.0, ThermalConductivityUnits.W_m_K);

            double tau = 1.0 - tempK / criticalTempK;
            if (tau < 0.0)
                return new ThermalConductivity(0.0, ThermalConductivityUnits.W_m_K);

            double k = _coeffs.C1 * (
                1.0
                + _coeffs.C2 * Math.Pow(tau, 1.0 / 3.0)
                + _coeffs.C3 * Math.Pow(tau, 2.0 / 3.0)
                + _coeffs.C4 * tau);

            return new ThermalConductivity(k, ThermalConductivityUnits.W_m_K);
        }
    }
}
