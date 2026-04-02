using Shared.Thermodynamics.Components;
using UnitSystem;

namespace Shared.DesignPatterns.PureComponents
{
    public class ExtendedAntoineEvaluator : IPropertyEvaluator<Temperature,Pressure>
    {
        private readonly CorrelationCoefficientsDto _coeffs;

      

        public ExtendedAntoineEvaluator(CorrelationCoefficientsDto coeffs) => _coeffs = coeffs;

        public Pressure EvaluateAt(Temperature temperature)
        {
            double tempKelvin = temperature.GetValue(TemperatureUnits.Kelvin);
            double tMin = _coeffs.Tmin.GetValue(TemperatureUnits.Kelvin);
            double tMax = _coeffs.Tmax.GetValue(TemperatureUnits.Kelvin);

            double tCalc = Math.Clamp(tempKelvin, tMin, tMax);

            double a = _coeffs.C1
                + (_coeffs.C2 / (_coeffs.C3 + tCalc))
                + (_coeffs.C4 * tCalc)
                + (_coeffs.C5 * Math.Log(tCalc))
                + (_coeffs.C6 * Math.Pow(tCalc, _coeffs.C7));

            double resultBar = Math.Exp(a);

            return new Pressure(resultBar, PressureUnits.Bar);
        }

        
    }

}
