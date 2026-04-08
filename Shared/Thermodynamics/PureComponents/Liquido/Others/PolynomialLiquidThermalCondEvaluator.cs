using Shared.PropertiesDtos.Components;
using Shared.Thermodynamics.PureComponents;
using UnitSystem;

namespace Shared.Thermodynamics.PureComponents.Liquido.Others
{
    // ---- LÍQUIDO ----
    public class PolynomialLiquidThermalCondEvaluator : IPropertyEvaluator<Temperature, ThermalConductivity>
    {
        private readonly CorrelationCoefficientsDto _coeffs;

        public PolynomialLiquidThermalCondEvaluator(CorrelationCoefficientsDto coeffs)
            => _coeffs = coeffs;

        public ThermalConductivity EvaluateAt(Temperature temperature)
        {
            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);
            double tMin = _coeffs.Tmin.GetValue(TemperatureUnits.Kelvin);
            double tMax = _coeffs.Tmax.GetValue(TemperatureUnits.Kelvin);

            if (tempK < tMin || tempK > tMax)
                return new ThermalConductivity(0.0, ThermalConductivityUnits.W_m_K);

            // Polinomial 4to grado
            double k = _coeffs.C1
                + _coeffs.C2 * tempK
                + _coeffs.C3 * Math.Pow(tempK, 2)
                + _coeffs.C4 * Math.Pow(tempK, 3)
                + _coeffs.C5 * Math.Pow(tempK, 4);

            return new ThermalConductivity(k, ThermalConductivityUnits.W_m_K);
        }
    }

}
