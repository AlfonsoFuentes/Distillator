using Shared.Thermodynamics.Components;
using UnitSystem;

namespace Shared.DesignPatterns.PureComponents.Liquido.Others
{
    // ---- LÍQUIDO ----
    public class PolynomialLiquidCpEvaluator : IPropertyEvaluator<Temperature, MolarEntropy>
    {
        private readonly CorrelationCoefficientsDto _coeffs;
      

        public PolynomialLiquidCpEvaluator(CorrelationCoefficientsDto coeffs)
        {
            _coeffs = coeffs;
     
        }

        public MolarEntropy EvaluateAt(Temperature temperature)
        {
            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);
            double tMin = _coeffs.Tmin.GetValue(TemperatureUnits.Kelvin);
            double tMax = _coeffs.Tmax.GetValue(TemperatureUnits.Kelvin);

            if (tempK < tMin || tempK > tMax)
                return new MolarEntropy(0.0, MolarEntropyUnits.KJ_Kgmol_C);

            // Polinomial 5to grado
            double cpMolar = _coeffs.C1
                + _coeffs.C2 * tempK
                + _coeffs.C3 * Math.Pow(tempK, 2)
                + _coeffs.C4 * Math.Pow(tempK, 3)
                + _coeffs.C5 * Math.Pow(tempK, 4);

            cpMolar /= 1000;

            return new MolarEntropy(cpMolar, MolarEntropyUnits.KJ_Kgmol_C);
        }
    }

}
