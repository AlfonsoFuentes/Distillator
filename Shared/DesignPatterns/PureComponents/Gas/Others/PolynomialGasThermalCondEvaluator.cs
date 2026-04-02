using Shared.DesignPatterns.PureComponents;
using Shared.Thermodynamics.Components;
using UnitSystem;

namespace Shared.DesignPatterns.PureComponents.Gas.Others
{
    // ---- GAS ----
    public class PolynomialGasThermalCondEvaluator : IPropertyEvaluator<Temperature, ThermalConductivity>
    {
        private readonly CorrelationCoefficientsDto _coeffs;

        public PolynomialGasThermalCondEvaluator(CorrelationCoefficientsDto coeffs)
            => _coeffs = coeffs;

        public ThermalConductivity EvaluateAt(Temperature temperature)
        {
            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);
            double tMin = _coeffs.Tmin.GetValue(TemperatureUnits.Kelvin);
            double tMax = _coeffs.Tmax.GetValue(TemperatureUnits.Kelvin);

            if (tempK < tMin || tempK > tMax)
                return new ThermalConductivity(0.0, ThermalConductivityUnits.W_m_K);

            // DIPPR 102
            double numerador = _coeffs.C1 * Math.Pow(tempK, _coeffs.C2);
            double denominador = 1 + _coeffs.C3 / tempK + _coeffs.C4 / Math.Pow(tempK, 2.0);

            return new ThermalConductivity(numerador / denominador, ThermalConductivityUnits.W_m_K);
        }
    }

}
