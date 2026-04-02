using Shared.Thermodynamics.Components;
using UnitSystem;

namespace Shared.DesignPatterns.PureComponents.Liquido.Others
{
    public class DipprHeatOfVaporizationEvaluator : IPropertyEvaluator<Temperature, MolarEnergy>
    {
        private readonly CorrelationCoefficientsDto _coeffs;
        private readonly Temperature _criticalTemperature;
       

        public DipprHeatOfVaporizationEvaluator(
            CorrelationCoefficientsDto coeffs, Temperature tc)
        {
            _coeffs = coeffs;
            _criticalTemperature = tc;
        
        }

        public MolarEnergy EvaluateAt(Temperature temperature)
        {
            double tempK = temperature.GetValue(TemperatureUnits.Kelvin);
            double tcK = _criticalTemperature.GetValue(TemperatureUnits.Kelvin);
            double tMin = _coeffs.Tmin.GetValue(TemperatureUnits.Kelvin);
            double tMax = _coeffs.Tmax.GetValue(TemperatureUnits.Kelvin);

            if (tempK <= tMin || tempK >= tMax || tcK <= 0)
                return new MolarEnergy(0.0, MolarEnergyUnits.J_Kgmol);

            double tr = tempK / tcK;

            double exponente = _coeffs.C2
                + _coeffs.C3 * tr
                + _coeffs.C4 * tr * tr
                + _coeffs.C5 * Math.Pow(tr, 3.0);

            // DIPPR 106 retorna J/kmol
            double hvapMolar = _coeffs.C1 * Math.Pow(1.0 - tr, exponente);

            return new MolarEnergy(hvapMolar, MolarEnergyUnits.J_Kgmol);
        }
    }

}
