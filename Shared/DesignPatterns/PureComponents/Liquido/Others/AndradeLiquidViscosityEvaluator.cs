using Shared.Thermodynamics.Components;
using UnitSystem;

namespace Shared.DesignPatterns.PureComponents.Liquido.Others
{
    //---- LÍQUIDO ----
    public class AndradeLiquidViscosityEvaluator : IPropertyEvaluator<Temperature, Viscosity>
    {
        private readonly CorrelationCoefficientsDto _coeffs;

        public AndradeLiquidViscosityEvaluator(CorrelationCoefficientsDto coeffs)
            => _coeffs = coeffs;

        public Viscosity EvaluateAt(Temperature temperature)
        {
            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);
            double tMin = _coeffs.Tmin.GetValue(TemperatureUnits.Kelvin);
            double tMax = _coeffs.Tmax.GetValue(TemperatureUnits.Kelvin);

            if (tempK < tMin || tempK > tMax)
                return new Viscosity(0.0, ViscosityUnits.Pa_s);

            // DIPPR 101
            double lnVisc = _coeffs.C1
                + _coeffs.C2 / tempK
                + _coeffs.C3 * Math.Log(tempK)
                + _coeffs.C4 * Math.Pow(tempK, _coeffs.C5);

            return new Viscosity(Math.Exp(lnVisc), ViscosityUnits.Pa_s);
        }
    }

}
