using Shared.PropertiesDtos.Components;
using Shared.Thermodynamics.PureComponents;
using UnitSystem;

namespace Shared.Thermodynamics.PureComponents.Gas.Others
{
    // ---- GAS ----
    public class DipprGasViscosityEvaluator : IPropertyEvaluator<Temperature, Viscosity>
    {
        private readonly CorrelationCoefficientsDto _coeffs;

        public DipprGasViscosityEvaluator(CorrelationCoefficientsDto coeffs)
            => _coeffs = coeffs;

        public Viscosity EvaluateAt(Temperature temperature)
        {
            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);
            double tMin = _coeffs.Tmin.GetValue(TemperatureUnits.Kelvin);
            double tMax = _coeffs.Tmax.GetValue(TemperatureUnits.Kelvin);

            if (tempK < tMin || tempK > tMax)
                return new Viscosity(0.0, ViscosityUnits.Pa_s);

            // DIPPR 102
            double numerador = _coeffs.C1 * Math.Pow(tempK, _coeffs.C2);
            double denominador = 1 + _coeffs.C3 / tempK + _coeffs.C4 / Math.Pow(tempK, 2.0);

            return new Viscosity(numerador / denominador, ViscosityUnits.Pa_s);
        }
    }

}
