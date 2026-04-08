using Shared.PropertiesDtos.Components;
using Shared.Thermodynamics.PureComponents;
using UnitSystem;

namespace Shared.Thermodynamics.PureComponents.Liquido.Others
{
    public class RackettLiquidDensityEvaluator : IPropertyEvaluator<Temperature, MolarDensity>
    {
        private readonly CorrelationCoefficientsDto _coeffs;
  

        public RackettLiquidDensityEvaluator(CorrelationCoefficientsDto coeffs)
        {
            _coeffs = coeffs;
            
        }

        public MolarDensity EvaluateAt(Temperature temperature)
        {
            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);
            double tMin = _coeffs.Tmin.GetValue(TemperatureUnits.Kelvin);
            double tMax = _coeffs.Tmax.GetValue(TemperatureUnits.Kelvin);

            if (tempK < tMin || tempK > tMax)
                return new MolarDensity(0.0, MolarDensityUnits.Kgmol_m3);

            // DIPPR 105
            double B = 1.0 + Math.Pow(1.0 - tempK / _coeffs.C3, _coeffs.C4);
            double exp = Math.Pow(_coeffs.C2, B);
            double densityMolar = _coeffs.C1 / exp;  // kmol/m³

            return new MolarDensity(densityMolar, MolarDensityUnits.Kgmol_m3);
        }
    }

}
