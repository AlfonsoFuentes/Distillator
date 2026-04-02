using Shared.Thermodynamics.Components;
using UnitSystem;

namespace Shared.DesignPatterns.PureComponents.Gas.Others
{
    // ---- GAS ----
    public class AlyLeeGasCpEvaluator : IPropertyEvaluator<Temperature, MolarEntropy>
    {
        private readonly CorrelationCoefficientsDto _coeffs;
      

        public AlyLeeGasCpEvaluator(CorrelationCoefficientsDto coeffs)
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

            double v1 = Math.Pow((_coeffs.C3 / tempK) / Math.Sinh(_coeffs.C3 / tempK), 2);
            double v2 = Math.Pow((_coeffs.C5 / tempK) / Math.Cosh(_coeffs.C5 / tempK), 2);

            // Cp molar en J/(mol·K) convertido a kJ/(kmol·K)
            double cpMolar = (_coeffs.C1 + _coeffs.C2 * v1 + _coeffs.C4 * v2) / 1000.0;

            return new MolarEntropy(cpMolar, MolarEntropyUnits.KJ_Kgmol_C);
        }
    }

}
